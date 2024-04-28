#pragma warning disable CS1591
namespace p3rpc.classconstructor.Interfaces;
public interface IClassMethods
{
    // Class extensions
    public unsafe void AddUnrealClassExtender(string targetClass, uint newSize, InternalConstructor? ctorHook = null);
    public unsafe delegate void InternalConstructor(nint alloc);
    // Class factories (will add public facing methods once this stuff actually works)
}