using System.Collections.Generic;

namespace Ccf.Ck.SysPlugins.Support.ActionQuery {
    public interface IActionQueryHostControl<ResolverValue> where ResolverValue: new() {
        /// <summary>
        /// When true calling Execute will cause Step to be called on each instruction.
        /// </summary>
        bool StartTrace();
        /// <summary>
        /// In trace mode this is called on each instruction
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="instruction"></param>
        /// <param name="arguments"></param>
        /// <returns>Should return true to continue, false to stop execution</returns>
        bool Step(int pc, Instruction instruction, ResolverValue[] arguments, IEnumerable<ResolverValue> stack = null);

    }
}