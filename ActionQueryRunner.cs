using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Ccf.Ck.SysPlugins.Support.ActionQuery
{
    public class ActionQueryRunner<ResolverValue> where ResolverValue: new() {
        
        private ActionQueryRunner(Instruction[] program) {
            _program = program;
        }
        private ActionQueryRunner(string error, Instruction[] program) {
            ErrorText = error;
            _program = program;
        }
        public bool IsValid {get { 
            return (ErrorText == null);
        }}
        public string ErrorText { get; private set; }
        private Instruction[] _program = null;
        
        #region Diagnostics

        public string DumpProgram() {
            return DumpProgram(_program);
        }
        public string DumpProgram(Instruction[] _instructions) {
            StringBuilder sb = new StringBuilder();
            if (_instructions != null) {
                for (int i = 0; i < _instructions.Length; i++) {
                    Instruction inst = _instructions[i];
                    sb.AppendFormat("{0}: {1} [{2}] ({3})", i, inst.Operation.ToString(), inst.Operand, inst.ArgumentsCount);
                    sb.AppendLine();
                }
            } else {
                sb.AppendLine("[no program]");
            }
            return sb.ToString();
        }
        #endregion


        public ResolverValue[] Execute(IActionQueryHost<ResolverValue> host) {
            int pc = 0, i;
            Stack<ResolverValue> _datastack = new Stack<ResolverValue>();
            //List<ResolverValue> _args = new List<ResolverValue>();
            ResolverValue[] _args = new ResolverValue[16];
            ResolverValue val;
            Instruction instr = Instruction.Empty;
            
            if (host == null) {
                throw new AuctionQueryException<ResolverValue>("No host given to Execute", Instruction.Empty, null, 0);
            }
            IActionQueryHostControl<ResolverValue> tracer = null;
            if (host is IActionQueryHostControl<ResolverValue> tr) {
                if (tr.StartTrace()) {
                    tracer = tr;
                }
            }

            try {
                while (pc < _program.Length) {
                    instr = _program[pc];
                    if (_datastack.Count < instr.ArgumentsCount) {
                        throw new AuctionQueryException<ResolverValue>("Stack underflow", instr, _datastack.ToArray(), pc);
                    } else if (instr.ArgumentsCount > 16) {
                        throw new AuctionQueryException<ResolverValue>("Too many arguments, up to 16 are supported.", instr, _datastack.ToArray(), pc);
                    }
                    for (i = instr.ArgumentsCount - 1; i >= 0;i--) {
                        _args[i] = _datastack.Pop();
                    }
                    if (tracer != null) {
                        if (!tracer.Step(pc, instr, _args, _datastack)) {
                            return null;
                        }
                    }
                    // The PC should be incremented after performing the instruction.
                    switch (instr.Operation) {
                        case Instructions.Call:
                            if (instr.Operand is string s) {
                                val = host.CallProc(s, _args.Take(instr.ArgumentsCount).ToArray());
                                _datastack.Push(val);
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Call requires string operand.", instr, _datastack.ToArray(),pc);    
                            }
                            pc++;
                            continue;
                        case Instructions.Jump:
                            if (instr.Operand is int x) {
                                if (x >= 0 && x < _program.Length) {
                                    pc = x;
                                    continue;
                                } else {
                                    throw new AuctionQueryException<ResolverValue>("Jump out of boundaries.", instr, _datastack.ToArray(),pc);    
                                }
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Jump has invalid operand.", instr, _datastack.ToArray(),pc);
                            }
                        case Instructions.JumpIfNot:
                            if (instr.Operand is int y) {
                                if (y >= 0 && y < _program.Length) {
                                    if (instr.ArgumentsCount > 0) {
                                        val = _args[0];
                                        if (host.IsTruthyOrFalsy(val)) {
                                            pc++;
                                            continue;
                                        } else {
                                            pc = y;
                                            continue;
                                        }
                                    } else {
                                        throw new AuctionQueryException<ResolverValue>("Not enough arguments for JumpIfNot.", instr, _datastack.ToArray(),pc);        
                                    }
                                } else {
                                    throw new AuctionQueryException<ResolverValue>("Jump out of boundaries.", instr, _datastack.ToArray(),pc);    
                                }
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Jump has invalid operand.", instr, _datastack.ToArray(),pc);
                            }
                        case Instructions.NoOp:
                            pc++;
                        continue;
                        case Instructions.PushBool:
                            if (instr.Operand is bool) {
                                _datastack.Push(host.FromBool((bool)instr.Operand));
                                pc++;
                                continue;
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Invalid operand type.", instr, _datastack.ToArray(),pc);
                            }
                        case Instructions.PushDouble:
                            if (instr.Operand is double) {
                                _datastack.Push(host.FromDouble((double)instr.Operand));
                                pc++;
                                continue;
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Invalid operand type.", instr, _datastack.ToArray(),pc);
                            }
                        case Instructions.PushInt:
                            if (instr.Operand is int) {
                                _datastack.Push(host.FromInt((int)instr.Operand));
                                pc++;
                                continue;
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Invalid operand type.", instr, _datastack.ToArray(),pc);
                            }
                        case Instructions.PushNull:
                            pc++;
                            _datastack.Push(host.FromNull());
                        continue;
                        case Instructions.PushParam:
                            if (instr.Operand is string) {
                                pc++;
                                val = host.EvalParam(instr.Operand as string);
                                _datastack.Push(val);
                                continue;
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Invalid operand type.", instr, _datastack.ToArray(),pc);
                            }
                        case Instructions.PushString:
                            if (instr.Operand is string) {
                                pc++;
                                _datastack.Push(host.FromString((string)instr.Operand));
                                continue;
                            } else {
                                throw new AuctionQueryException<ResolverValue>("Invalid operand type.", instr, _datastack.ToArray(),pc);
                            }
                        case Instructions.Dump:
                            pc++;
                            continue;
                        default:
                            throw new AuctionQueryException<ResolverValue>("Unsupported instruction.", instr, _datastack.ToArray(),pc);
                    }
                }
                // Execution finished
                if (_datastack.Count > 0) {
                    return _datastack.ToArray();
                } else {
                    return null;
                }
            } catch (AuctionQueryException<ResolverValue> ex) {
                throw ex;
            } catch (Exception ex) {
                throw new AuctionQueryException<ResolverValue>("Exception in the ActionQuery's host.", instr, _datastack.ToArray(),pc, ex);
            }
        }
        public ResolverValue ExecuteScalar(IActionQueryHost<ResolverValue> host) {
            var r = Execute(host);
            if (r != null) {
                return r[r.Length - 1];
            } else {
                return new ResolverValue();
            }
        }


        public class Constructor {
            private List<Instruction> _instructions = new List<Instruction>();
            public Constructor() {}

            public Constructor Add(Instruction instruction) {
                _instructions.Add(instruction);
                return this;
            }
            public bool Update(int address, object operand) {
                if (address >= 0 && address < _instructions.Count) {
                    var instr =_instructions[address];
                    instr.Operand = operand;
                    _instructions[address] = instr;
                }
                return false;
            }
            public int Address {
                get {
                    return _instructions.Count;
                }
            }
            public ActionQueryRunner<ResolverValue> Complete(string err = null) {
                if (err != null) {
                    return new ActionQueryRunner<ResolverValue>(err, _instructions.ToArray());
                } else {
                    Add(new Instruction(Instructions.NoOp)); // To accommodate jumps to the end.
                    return new ActionQueryRunner<ResolverValue>(_instructions.ToArray());
                }
            }
        }

        

    }
}