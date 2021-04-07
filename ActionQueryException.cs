using System;

namespace Ccf.Ck.SysPlugins.Support.ActionQuery
{
    public class AuctionQueryException<ResolverValue>: Exception where ResolverValue: new() {
        public AuctionQueryException(string description,Instruction instruction, ResolverValue[] stack,int pc, Exception inner = null):base(description,inner) {
            this.Data.Add("AQ STACK", stack);
            Data.Add("Instruction", instruction);
            Data.Add("PC", pc);
        }
    }
}