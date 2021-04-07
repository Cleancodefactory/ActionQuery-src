namespace Ccf.Ck.SysPlugins.Support.ActionQuery {
    public interface IActionQueryHost<ResolverValue> where ResolverValue: new() {
        /// <summary>
        /// Must return null encoded as the parameter type
        /// </summary>
        /// <returns>null-y value</returns>
        ResolverValue FromNull();
        /// <summary>
        /// Translates boolean value to the generic type
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        ResolverValue FromBool(bool arg);
        /// <summary>
        /// Translates double to the generic type
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        ResolverValue FromDouble(double arg);
        /// <summary>
        /// Translates integer to the generic parameter type
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        ResolverValue FromInt(int arg);
        /// <summary>
        /// Translates string to the generic parameter type
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        ResolverValue FromString(string arg);
        /// <summary>
        /// Returns the value of the named parameter. The parameters are externally supplied named values and
        /// how they are resolved against names is up to the host.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        ResolverValue EvalParam(string param);
        /// <summary>
        /// Estimates a generic parameter typed value as thruthy or falsy (if and while depend on this method)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        bool IsTruthyOrFalsy(ResolverValue v);
        /// <summary>
        /// Called when Call instruction is executed. This usually translates to calls of own methods of the host, but can 
        /// be implemented in any compatible way, including support for libraries in which the host can look up for the method.
        /// The arguments are determined by the query, thus the actual implementations can support variable number of arguments which is 
        /// especially simple to implement if they accept arrays/params.
        /// 
        /// All methods MUST return a value. Depending on how the ActionQuery would be used this means that having an empty value could be 
        /// convenient. To achieve that the generic type should support it in some way (it is entirely your own decision how). 
        /// 
        /// Exceptions: The basic way to handle this is to not handle it. In such a case the AC will throw an ActionQueryException with yours as 
        /// inner exception.
        /// </summary>
        /// <param name="method">The name of the method as stated in the query. The operand of the instruction.</param>
        /// <param name="args">Array of arguments. Their number is determined by the query e.g. xfunc(arg1,arg2,arg3).</param>
        /// <returns></returns>
        ResolverValue CallProc(string method, ResolverValue[] args);

    }
}