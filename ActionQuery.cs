using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ccf.Ck.Libs.ActionQuery
{
    public class ActionQuery<ResolverValue> where ResolverValue: new() {

        enum Terms {
            
            none = 0,
            // Space is found - usually ignored
            space = 1,
            // special literals for specific values - true, false, null, value
            specialliteral = 2,
            // Operator from the langlet
            keyword = 3,
            // identifier - function name or parameter name to fetch (the actual fetching depends on the usage)
            // Addresses 0 - just found, 1 - end of first arg, last - 1 - last arg, last - after final element
            varidentifier = 4,
            identifier = 5,
            // Open normal bracket (function call arguments, grouping is not supported intentionally - see the docs for more details)
            openbracket = 6,
            // close normal bracket - end of function call argument list.
            closebracket = 7,
            // string literal 'something'
            stringliteral = 8,
            // numeric literal like: 124, +234, -324, 123.45, -2.43, +0.23423 etc.
            numliteral = 9,
            // comma separator of arguments. can be used at top level also, in this case this will produce multiple results (usable only with the corresponding evaluation routines)
            comma = 10,
            // end of the expression
            end = 11,
            // Virtual tokens ===
            comment = 12,
            
            compound = 101
        
        }
        //private static readonly Regex _regex = new Regex(@"(\s+)|(true|false|null)|(while|if)|(?:\$([a-zA-Z0-9_\.\-]+))|([a-zA-Z_][a-zA-Z0-9_\.\-]*)|(\()|(\))|(?:\'((?:\\'|[^\'])*)\')|([\+\-]?\d+(?:\.\d*)?)|(\,|(?:\r|\n)+)|($)|(#.*?(?:\n|\r)+)",
        //    RegexOptions.None);
        private static readonly Regex _regex = new Regex(@"(\s+)|(true|false|null)|(while|if|halt)|(?:\$([a-zA-Z0-9_\.\-]+))|([a-zA-Z_][a-zA-Z0-9_\.\-]*)|(\()|(\))|(?:\'((?:\\'|[^\'])*)\')|([\+\-]?\d+(?:\.\d*)?)|(,)|($)|(#.*?(?:\n|\r)+)",
            RegexOptions.None);


        private class OpEntry {
            internal OpEntry(string v, Terms t,int pos = -1) {
                Value = v;
                Term = t;
                Pos = pos;
                Arguments = 0;
                Op1Address = Op0Address = Op2Address = -1; // invalid
            }
            internal string Value;
            internal Terms Term;
            internal int Pos;

            internal int Arguments;

            

            internal int Op0Address; // The address of the start (while)
            internal int Op1Address; // The address of the control jump instruction
            internal int Op2Address; // The address of the second jump instruction (if else)

            public bool IsEmpty { get {
                return (Term == Terms.none);
            }}
            public static OpEntry Empty {
                get {
                    return new OpEntry(null, Terms.none);
                }
            }

        }

        #region Internal helpers (can be implemented as local methods, but I hate it)
        
        private int ParsePos(Match m) {
            return m.Index;
        }
        private string ReportError(string fmt,Match m, string source) {
            
            if (m != null) {
                return ReportError(fmt, ParsePos(m),source);
            } 
            return fmt;
        }
        private string ReportError(string fmt,int m, string source) {
            if (m >= 0 && m < source.Length) {
                if (source != null) {
                    return $"{string.Format(fmt,m)} source with the error marked follows:\n{source.Insert(m, "[***ERROR***]")}";    
                }
                return string.Format(fmt,m);
            } 
            return fmt;
        }
        private void AddArg(Stack<OpEntry> stack, ActionQueryRunner<ResolverValue>.Constructor constr) {
            if (stack.Count > 0) {
                var entry = stack.Peek();
                entry.Arguments ++;

            }
        }
        #endregion
        public ActionQueryRunner<ResolverValue> Compile(string query) {

            Stack<OpEntry> opstack = new Stack<OpEntry>();
            ActionQueryRunner<ResolverValue>.Constructor runner = new ActionQueryRunner<ResolverValue>.Constructor();
            OpEntry undecided = OpEntry.Empty;
            OpEntry entry;
            int pos = 0; // used and updated only for additional error checking. The algorithm does not depend on this.
            int level = 0;
            

            Match match = _regex.Match(query);
            while(match.Success) {
                if (pos != match.Index) return runner.Complete(ReportError("Syntax error at {0} - unrecognized text",match.Index, query));
                pos = match.Index + match.Length;
                if (match.Groups[0].Success) {
                    for (int i = 1; i < match.Groups.Count; i++) {
                        if (match.Groups[i].Success) {
                            string curval = match.Groups[i].Value;
                            switch ((Terms)i) {
                                case Terms.keyword:
                                    if (!undecided.IsEmpty) {
                                        return runner.Complete(ReportError("Syntax error at {0}.", match, query));
                                    }
                                    if (curval == "halt") {
                                        runner.Add(new Instruction(Instructions.Halt)); 
                                    } else {
                                        undecided = new OpEntry(curval, Terms.keyword, match.Index);
                                    }
                                goto nextTerm;
                                case Terms.varidentifier:
                                    if (!undecided.IsEmpty) {
                                        return runner.Complete(ReportError("Syntax error at {0}.", match, query));
                                    }
                                    undecided = new OpEntry(curval,Terms.varidentifier,match.Index);
                                goto nextTerm;
                                case Terms.identifier:
                                    if (!undecided.IsEmpty) {
                                        return runner.Complete(ReportError("Syntax error at {0}.", match, query));
                                    }
                                    undecided = new OpEntry(curval,Terms.identifier,match.Index);
                                goto nextTerm;
                                case Terms.openbracket:
                                    if (undecided.Term == Terms.varidentifier) {
                                        opstack.Push(undecided); // Var set
                                        undecided = OpEntry.Empty;
                                    } else if (undecided.Term == Terms.identifier) {
                                        opstack.Push(undecided); // Function call
                                        undecided = OpEntry.Empty;
                                    } else if (undecided.Term == Terms.keyword) {
                                        undecided.Op0Address = runner.Address;
                                        opstack.Push(undecided);
                                        undecided = OpEntry.Empty;
                                    } else if (undecided.IsEmpty) {
                                        opstack.Push(new OpEntry(null, Terms.compound,match.Index));
                                    }
                                    level ++;
                                goto nextTerm;
                                case Terms.closebracket:
                                    if (undecided.Term == Terms.varidentifier) {
                                        AddArg(opstack, runner);
                                        runner.Add(new Instruction(Instructions.PushVar,undecided.Value)); // GetVar varidentifier
                                        undecided = OpEntry.Empty;
                                    } else if (undecided.Term == Terms.identifier) {
                                        AddArg(opstack, runner);
                                        runner.Add(new Instruction(Instructions.PushParam,undecided.Value));
                                        undecided = OpEntry.Empty;
                                        
                                    }
                                    // *** Function call
                                    if (opstack.Count == 0) return runner.Complete(ReportError("Syntax error - function call has no function name at {0}",match, query));
                                    entry = opstack.Pop();
                                    if (entry.Term == Terms.varidentifier) {
                                        AddArg(opstack, runner);
                                        // TODO - what about empty argument list? (BUG)
                                        runner.Add(new Instruction(Instructions.PullVar, entry.Value,entry.Arguments));
                                    } else if (entry.Term == Terms.identifier) {
                                        AddArg(opstack, runner);
                                        // TODO - what about empty argument list? (BUG)
                                        runner.Add(new Instruction(Instructions.Call, entry.Value,entry.Arguments));
                                    } else if (entry.Term == Terms.keyword) {
                                        AddArg(opstack, runner);
                                        // TODO: Operator completion
                                        if (entry.Value == "if") {
                                            if (entry.Arguments == 2) {
                                                runner.Add(new Instruction(Instructions.Jump, runner.Address + 2));
                                                
                                                // Update initial jumpIfNot
                                                runner.Update(entry.Op1Address, runner.Address);
                                                // Add dummy else
                                                runner.Add(new Instruction(Instructions.PushNull));
                                            } else if (entry.Arguments == 3) {
                                                // Update else unconditional jump
                                                runner.Update(entry.Op2Address, runner.Address);
                                            } else {
                                                return runner.Complete(ReportError("if must have 2 or 3 arguments at {0}", match,query));
                                            }
                                        } else if (entry.Value == "while") {
                                            if (entry.Arguments == 2) {
                                                runner.Add(new Instruction(Instructions.Dump,null, 1));
                                                runner.Add(new Instruction(Instructions.Jump, entry.Op0Address)); // Jump to the initial condition
                                                runner.Update(entry.Op1Address, runner.Address); // Update initial JumpIfNot to go after the end
                                                runner.Add(new Instruction(Instructions.PushNull)); // Push something to keep the illusion that something is returned.
                                            } else {
                                                return runner.Complete(ReportError("while must have 2 arguments at {0}", match, query));
                                            }
                                        } else {
                                            return runner.Complete(ReportError("Unexpected end of control operator at {0}", match, query));
                                        }
                                    } else if (entry.Term == Terms.compound) {
                                        AddArg(opstack, runner);
                                    } else {
                                        return runner.Complete(ReportError("Syntax error - function call has no function name at {0}",match, query));
                                    }
                                    level --;
                                goto nextTerm;
                                case Terms.comma:
                                    if (undecided.Term == Terms.varidentifier) {
                                        AddArg(opstack, runner);
                                        runner.Add(new Instruction(Instructions.PushVar, undecided.Value));
                                        undecided = OpEntry.Empty;
                                    } else if (undecided.Term == Terms.identifier) {
                                        AddArg(opstack, runner);
                                        runner.Add(new Instruction(Instructions.PushParam, undecided.Value));
                                        undecided = OpEntry.Empty;
                                        
                                    } else if (!undecided.IsEmpty) { // If this happend it will be our mistake. Nothing but identifiers should appear in the undecided
                                        return runner.Complete(ReportError("Syntax error at {0}",undecided.Pos,query));
                                    }
                                    // TODO: Consider root level behavior! Multiple results may be useful?
                                    if (opstack.Count == 0 || opstack.Peek().Term == Terms.compound) {
                                        // A coma in compond operator or on root level - only the last one must remain in the stack
                                        // For this reason we dump the last entry.
                                        runner.Add(new Instruction(Instructions.Dump, null, 1));
                                    } else if (opstack.Peek().Term == Terms.keyword) {
                                        // TODO: operator midtime
                                        entry = opstack.Peek();
                                        if (entry.Arguments == 1) {
                                            // We are at addr(1)
                                            entry.Op1Address = runner.Address;
                                            runner.Add(new Instruction(Instructions.JumpIfNot, -1, 1));
                                        } else if (entry.Arguments == 2) {
                                            if (entry.Op1Address >= 0) {
                                                if (entry.Value == "if") {
                                                    // Finish successful part
                                                    entry.Op2Address = runner.Address;
                                                    runner.Add(new Instruction(Instructions.Jump, -1));
                                                    // Jump here if condition is not met
                                                    runner.Update(entry.Op1Address, runner.Address);
                                                    //AddArg(opstack, runner);
                                                } else if (entry.Value == "while") {
                                                    return runner.Complete(ReportError("while has more than two arguments at {0}", match,query));
                                                } else {
                                                    // This is completely unexpected
                                                    return runner.Complete(ReportError("syntax error at {0}", match,query));
                                                }
                                            } else {
                                                return runner.Complete(ReportError("while or if operator cannot be composed correctly {0}",undecided.Pos,query));
                                            }
                                        }
                                    }
                                goto nextTerm;
                                case Terms.numliteral:
                                    if (!undecided.IsEmpty) return runner.Complete(ReportError("Syntax error at {0}",undecided.Pos, query));
                                    if (curval.IndexOf('.') >= 0) { // double
                                        if (double.TryParse(curval,NumberStyles.Any,CultureInfo.InvariantCulture, out double t)) {
                                            runner.Add(new Instruction(Instructions.PushDouble, t));
                                            AddArg(opstack, runner);
                                        } else {
                                            return runner.Complete(ReportError("Invalid double number at {0}",match,query));
                                        }
                                    } else {
                                        if (int.TryParse(curval,NumberStyles.Any,CultureInfo.InvariantCulture, out int n)) {
                                            runner.Add(new Instruction(Instructions.PushInt,n));
                                            AddArg(opstack, runner);
                                        } else {
                                            return runner.Complete(ReportError("Invalid integer number at {0}",match,query));
                                        }
                                    }
                                goto nextTerm;
                                case Terms.specialliteral:
                                    if (!undecided.IsEmpty) return runner.Complete(ReportError("Syntax error at {0}",undecided.Pos,query));
                                    if (curval == "null") {
                                        runner.Add(new Instruction(Instructions.PushNull));
                                    } else if (curval == "true") {
                                        runner.Add(new Instruction(Instructions.PushBool,true));
                                    } else if (curval == "false") {
                                        runner.Add(new Instruction(Instructions.PushBool,false));
                                    } else {
                                        return runner.Complete(ReportError("Syntax error at {0}",match,query));
                                    }
                                    AddArg(opstack, runner);
                                goto nextTerm;
                                case Terms.stringliteral:
                                    if (!undecided.IsEmpty) {
                                        return runner.Complete(ReportError("Syntax error at {0}", undecided.Pos,query));
                                    }
                                    runner.Add(new Instruction(Instructions.PushString,curval));
                                    AddArg(opstack, runner);
                                goto nextTerm;
                                case Terms.space:
                                case Terms.comment:
                                    // do nothing - we simply ignore the space
                                goto nextTerm;
                                case Terms.end:
                                    if (undecided.Term == Terms.varidentifier) {
                                        AddArg(opstack, runner);
                                        runner.Add(new Instruction(Instructions.PushVar, undecided.Value));
                                        undecided = OpEntry.Empty;
                                    } else if (undecided.Term == Terms.identifier) {
                                        AddArg(opstack, runner);
                                        runner.Add(new Instruction(Instructions.PushParam, undecided.Value));
                                        undecided = OpEntry.Empty;
                                    }
                                    if (opstack.Count == 0) {
                                        // The stack must be empty at this point
                                        return runner.Complete();
                                    } else {
                                        return runner.Complete("Syntax error at the expression end - check for matching brackets");
                                    }
                                // break;
                                default:
                                    return runner.Complete(ReportError("Syntax error at {0}",match, query));
                                
                            }
                        } // catch actual group
                    } // Check every possible group
                } else {
                    // Unrecognized or end
                }
                nextTerm:
                match = match.NextMatch();
            } // next term
            var _serr = "";
            if (pos > query.Length) {
                _serr = " Unparsed:[" + query.Substring(pos) + "]";
            }
            return runner.Complete("Parsing the query failed at pos:" + pos + _serr );
        }
    }
}
