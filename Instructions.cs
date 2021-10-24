namespace Ccf.Ck.Libs.ActionQuery {
    /// <summary>
    /// This is public to facilitate diagnostics
    /// </summary>
    public enum Instructions {
        NoOp = 0, // () - does nothing
        PushParam = 1, // (parameterName) - pushes outer parameter's value in the stack

        Call = 2, // (methodName) - Calls a routine (provided by the host)
        PushDouble = 3, // (double) - Pushes a double on the stack
        PushInt = 4, // (int) - Pushes an int on the stack
        PushNull = 5, // () - Pushes null on the stack
        PushBool = 6, // (bool) - Pushes a boolean on the stack
        PushString = 7, // (string) - pushes a string on the stack
        Dump = 8, // () - Pulls and dumps (forgets) one entry from the stack
        JumpIfNot = 9, // (jumpaddress), 1 arg
        Jump = 10, // (jumpaddress), 0 arg

        #region 1.1
        GetVar = 11, // $varname - acquires variable by varname from the host and puts its value on the stack
        SetVar = 12 // $varname(expression) - sets the variable varname in the host
        #endregion 1.1

    }
}