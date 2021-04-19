using System;
using System.Collections.Generic;

namespace Ccf.Ck.Libs.ActionQuery
{

    public class ActionQueryException<ResolverValue>: Exception where ResolverValue: new() {
        private const string INSTRUCTION = "Instruction";
        private const string AQSTACK = "AQ STACK";
        private const string PC = "PC";
        public ActionQueryException(string description,Instruction instruction, ResolverValue[] stack,int pc, Exception inner = null):base(description,inner) {
            this.Data.Add(AQSTACK, stack);
            Data.Add(INSTRUCTION, instruction);
            Data.Add(PC, pc);
        }

        #region Obtain specifics
        public IEnumerable<ResolverValue> AQStack {
            get {
                if (this.Data.Contains(AQSTACK)) {
                    var stack = this.Data[AQSTACK] as ResolverValue[];
                    if (stack != null) return stack;
                }
                return null;
            }
        }
        public Instruction Instruction {
            get {
                if (this.Data.Contains(INSTRUCTION)  && this.Data[INSTRUCTION] is Instruction) {
                    return (Instruction)this.Data[INSTRUCTION];
                }
                return default(Instruction);
            }
        }
        public int Pc {
            get {
                if (this.Data.Contains(PC) && this.Data[PC] is int n) {
                    return n;
                }
                return -1;
            }
        }
        #endregion

    }

    
}