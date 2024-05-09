#pragma warning disable CS1591
using System.Runtime.InteropServices;

public abstract class ProcessEventParameterBase
{
    public string Name { get; protected set; }
    public nint Value { get; protected set; }
    public nint Size { get; protected set; }

    protected ProcessEventParameterBase(string name, nint value, nint size)
    {
        Name = name;
        Value = value;
        Size = size;
    }

    public abstract void AddToAllocation(nint pos);
    public abstract void RetrieveValue(nint pParams);
}

// Pass a param to a blueprints function by value
// ( e.g int, float, any pointer/reference such as FName& or FVector* )
public class ProcessEventParameterValue : ProcessEventParameterBase
{
    private nint ValueMask { get; set; }
    public ProcessEventParameterValue(string name, nint value, nint size) : base(name, value, size)
    {
        ValueMask = (1 << (int)(size * 8)) - 1;
    }
    public unsafe override void AddToAllocation(nint pos)
    {
        *(nint*)pos &= ~ValueMask; // only clear bits overlapping current field
        *(nint*)pos |= Value;
    }
    // Since it's not a reference, we can't copy anything
    public override void RetrieveValue(nint pParams) {}
}
// Pass a param to a blueprints function by reference
// When adding to param block in ProcessEvent, a deref occurs to copy the target data
public class ProcessEventParameterPointer : ProcessEventParameterBase
{
    public unsafe ProcessEventParameterPointer(string name, nint value, nint size) : base(name, value, size) { }
    public unsafe override void AddToAllocation(nint pos) => NativeMemory.Copy((void*)Value, (void*)pos, (nuint)Size);
    public override void RetrieveValue(nint pParams) { }
}

public class ProcessEventParameterReference : ProcessEventParameterBase
{
    private nint ValueMask { get; set; }
    public unsafe ProcessEventParameterReference(string name, nint value, nint size) : base(name, value, size) 
    {
        ValueMask = (1 << (int)(size * 8)) - 1;
    }
    public unsafe override void AddToAllocation(nint pos) => NativeMemory.Copy((void*)Value, (void*)pos, (nuint)Size);
    public unsafe override void RetrieveValue(nint pParams) 
    {
        var outVal = *(nint*)pParams & ValueMask;
        *(nint*)Value &= ~ValueMask; // only clear bits overlapping current field
        *(nint*)Value |= outVal;
    }
}

public static class ProcessEventParameterFactory
{
    public static ProcessEventParameterBase MakeByteParameter(string pName, byte In)
        => new ProcessEventParameterValue(pName, In, 1);
    public static ProcessEventParameterBase MakeShortParameter(string pName, short In)
        => new ProcessEventParameterValue(pName, In, 2);
    public static ProcessEventParameterBase MakeUshortParameter(string pName, ushort In)
        => new ProcessEventParameterValue(pName, In, 2);
    public static ProcessEventParameterBase MakeIntParameter(string pName, int In)
        => new ProcessEventParameterValue(pName, In, 4);
    public static ProcessEventParameterBase MakeUintParameter(string pName, uint In)
        => new ProcessEventParameterValue(pName, (nint)In, 4);
    public static ProcessEventParameterBase MakeFloatParameter(string pName, float In)
        => new ProcessEventParameterValue(pName, BitConverter.SingleToInt32Bits(In), 4);
    public unsafe static ProcessEventParameterBase MakeStructParameterRef<TType>(string pName, TType* value) where TType : unmanaged
        => new ProcessEventParameterValue(pName, (nint)value, sizeof(TType*));
    public unsafe static ProcessEventParameterBase MakeStructParameterValue<TType>(string pName, TType* value) where TType : unmanaged
        => new ProcessEventParameterPointer(pName, (nint)value, sizeof(TType));
    public unsafe static ProcessEventParameterBase MakeStructParameterOut<TType>(string pName, TType* value) where TType : unmanaged
        => new ProcessEventParameterReference(pName, (nint)value, sizeof(TType));
}