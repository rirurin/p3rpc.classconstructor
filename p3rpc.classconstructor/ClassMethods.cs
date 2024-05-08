using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using static p3rpc.classconstructor.Interfaces.IClassMethods;
using System.Collections.Concurrent;
using Reloaded.Hooks.Definitions;
namespace p3rpc.classconstructor
{
    internal class ClassMethods : ModuleBase<ClassConstructorContext>, IClassMethods
    {
        public ConcurrentDictionary<string, ClassExtenderParams> _classNameToClassExtender { get; private init; } = new();
        public Dictionary<string, nint> _classNameToType { get; private init; } = new();
        public ClassMethods(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) { }
        public override void Register() { }

        // This should be done on during module ctor, which will run before any Unreal Engine code runs
        public unsafe void AddUnrealClassExtender
            (string targetClass, uint newSize,
            InternalConstructor? ctorHook = null) // called when UE runs InternalConstructor_[TARGET_CLASS_NAME]
                                                  //Action<IHook<ClassExtenderParams.InternalConstructor>>? onMakeHook = null) // for the caller to store the created hook
            => _classNameToClassExtender.TryAdd(targetClass, new ClassExtenderParams(newSize, ctorHook));
    }

    // Extend a particular class (increase size, hook to ctor)
    public class ClassExtenderParams
    {
        public uint Size { get; init; }
        public InternalConstructor? CtorHook { get; set; }
        public IHook<InternalConstructor>? CtorHookReal { get; set; }
        public ClassExtenderParams
            (uint size, InternalConstructor? ctorHook)
        {
            Size = size;
            CtorHook = ctorHook;
            CtorHookReal = null;
        }
    }
}
