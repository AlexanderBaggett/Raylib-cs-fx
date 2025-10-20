namespace Raylib_cs_fx
{
    /// <summary>
    /// Too much perforamnce overhead for a hot loop with 10K+ values
    /// tends to lengthen the callstack too much
    /// Better for values that don't change frequently
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct FuncOrVal<T>
    {
        private readonly T? value;
        private readonly Func<T>? funcVal;

        public FuncOrVal(T value)
        {
            this.value = value;
        }
        public FuncOrVal(Func<T> func)
        {
            this.funcVal = func;
        }

        public static implicit operator FuncOrVal<T>(T value)
        {
            return new FuncOrVal<T>(value);
        }
        public static implicit operator FuncOrVal<T>(Func<T> func)
        {
            return new FuncOrVal<T>(func);
        }

        //Because there are only 2 constructors one of these must be not null
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public T Value => value ?? funcVal();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    }
}
