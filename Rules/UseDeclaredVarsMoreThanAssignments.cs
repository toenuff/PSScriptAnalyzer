﻿//
// Copyright (c) Microsoft Corporation.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Globalization;

namespace Microsoft.Windows.PowerShell.ScriptAnalyzer.BuiltinRules
{
    /// <summary>
    /// UseDeclaredVarsMoreThanAssigments: Analyzes the ast to check that variables are used in more than just their assignment.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseDeclaredVarsMoreThanAssigments : IScriptRule
    {
        /// <summary>
        /// AnalyzeScript: Analyzes the ast to check that variables are used in more than just there assignment.
        /// </summary>
        /// <param name="ast">The script's ast</param>
        /// <param name="fileName">The script's file name</param>
        /// <returns>A List of results from this rule</returns>
        public IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string fileName)
        {
            if (ast == null) throw new ArgumentNullException(Strings.NullAstErrorMessage);

            IEnumerable<Ast> assignmentAsts = ast.FindAll(testAst => testAst is AssignmentStatementAst, true);            
            IEnumerable<Ast> varAsts = ast.FindAll(testAst => testAst is VariableExpressionAst, true);
            IEnumerable<Ast> varsInAssignment;

            Dictionary<string, AssignmentStatementAst> assignments = new Dictionary<string, AssignmentStatementAst>(StringComparer.OrdinalIgnoreCase);

            string varKey;
            bool inAssignment;

            if (assignmentAsts != null)
            {
                foreach (AssignmentStatementAst assignmentAst in assignmentAsts)
                {
                    // Only checks for the case where lhs is a variable. Ignore things like $foo.property
                    VariableExpressionAst assignmentVarAst = assignmentAst.Left as VariableExpressionAst;

                    if (assignmentVarAst != null)
                    {
                        //Ignore if variable is global or environment variable
                        if (!Helper.Instance.IsVariableGlobalOrEnvironment(assignmentVarAst, ast)
                            && !assignmentVarAst.VariablePath.IsScript)
                        {
                            if (!assignments.ContainsKey(assignmentVarAst.VariablePath.UserPath))
                            {
                                assignments.Add(assignmentVarAst.VariablePath.UserPath, assignmentAst);
                            }
                        }
                    }
                }
            }

            if (varAsts != null)
            {
                foreach (VariableExpressionAst varAst in varAsts)
                {
                    varKey = varAst.VariablePath.UserPath;
                    inAssignment = false;
                    
                    if (assignments.ContainsKey(varKey))
                    {
                            varsInAssignment = assignments[varKey].Left.FindAll(testAst => testAst is VariableExpressionAst, true); ;

                            //Checks if this variableAst is part of the logged assignment
                            foreach (VariableExpressionAst varInAssignment in varsInAssignment)
                            {
                                inAssignment |= varInAssignment.Equals(varAst);
                            }

                            if (!inAssignment)
                            {
                                assignments.Remove(varKey);
                            }
                            //Check if variable belongs to PowerShell builtin variables
                            if (Helper.Instance.HasSpecialVars(varKey))
                            {
                                assignments.Remove(varKey);
                            }
                    }
                }
            }

            foreach (string key in assignments.Keys)
            {
                yield return new DiagnosticRecord(string.Format(CultureInfo.CurrentCulture, Strings.UseDeclaredVarsMoreThanAssignmentsError, key),
                    assignments[key].Extent, GetName(), DiagnosticSeverity.Warning, fileName, key);
            }
        }

        /// <summary>
        /// GetName: Retrieves the name of this rule.
        /// </summary>
        /// <returns>The name of this rule</returns>
        public string GetName()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.NameSpaceFormat, GetSourceName(), Strings.UseDeclaredVarsMoreThanAssignmentsName);
        }

        /// <summary>
        /// GetCommonName: Retrieves the common name of this rule.
        /// </summary>
        /// <returns>The common name of this rule</returns>
        public string GetCommonName()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.UseDeclaredVarsMoreThanAssignmentsCommonName);
        }

        /// <summary>
        /// GetDescription: Retrieves the description of this rule.
        /// </summary>
        /// <returns>The description of this rule</returns>
        public string GetDescription()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.UseDeclaredVarsMoreThanAssignmentsDescription);
        }

        /// <summary>
        /// GetSourceType: Retrieves the type of the rule: builtin, managed or module.
        /// </summary>
        public SourceType GetSourceType()
        {
            return SourceType.Builtin;
        }

        /// <summary>
        /// GetSeverity: Retrieves the severity of the rule: error, warning of information.
        /// </summary>
        /// <returns></returns>
        public RuleSeverity GetSeverity()
        {
            return RuleSeverity.Warning;
        }

        /// <summary>
        /// GetSourceName: Retrieves the module/assembly name the rule is from.
        /// </summary>
        public string GetSourceName()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.SourceName);
        }
    }

}
