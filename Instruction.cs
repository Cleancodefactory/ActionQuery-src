namespace Ccf.Ck.SysPlugins.Support.ActionQuery {
    public struct Instruction {
        public Instruction(Instructions operation, object operand = null, int argcount = 0) {
            Operation = operation;
            Operand = operand;
            ArgumentsCount = argcount;
        }
        public Instructions Operation {get; private set;}
        public object Operand;
        public int ArgumentsCount { get; private set;}

        public static Instruction Empty {
            get {
                return new Instruction(Instructions.NoOp, null,0);
            }
        }
    }
}