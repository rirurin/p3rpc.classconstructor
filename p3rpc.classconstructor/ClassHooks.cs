using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;
using static p3rpc.classconstructor.Interfaces.IClassMethods;
using static p3rpc.nativetypes.Interfaces.IUIMethods;

namespace p3rpc.classconstructor
{
    internal class ClassHooks : ModuleBase<ClassConstructorContext>
    {
        public IHook<StaticConstructObject_Internal> _staticConstructObject { get; private set; }
        public IHook<GetPrivateStaticClassBody> _staticClassBody { get; private set; }

        public unsafe delegate UObject* StaticConstructObject_Internal(FStaticConstructObjectParameters* pParams);

        public unsafe delegate void GetPrivateStaticClassBody(nint packageName, nint name, UClass** returnClass, nint registerNativeFunc, uint size, uint align, uint flags, ulong castFlags, nint config, nint inClassCtor, nint vtableHelperCtorCaller, nint addRefObjects, nint superFn, nint withinFn, byte isDynamic, nint dynamicFn);
        public UClass_DeferredRegister _deferredRegister { get; private set; }
        private string UClass_DeferredRegister_SIG = "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8B 93 ?? ?? ?? ?? 48 8D 4C 24 ??";
        public unsafe delegate void UClass_DeferredRegister(UClass* self, UClass* type, nint packageName, nint name);

        public UObjectProcessRegistrants _processRegistrants { get; private set; }
        private string UObjectProcessRegistrants_SIG = "48 8B C4 55 48 83 EC 70 48 89 58 ?? 48 8D 15 ?? ?? ?? ??";
        public delegate void UObjectProcessRegistrants();

        public FName_Ctor _fnameCtor;
        private string FNameCtor_SIG = "48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 48 89 54 24 ?? 33 C9 41 8B F8 4C 8B DA";
        public unsafe delegate FName* FName_Ctor(FName* self, nint name, EFindType findType);

        /* 
        public UClass_FindFunctionByName _findFunctionByName;
        private string UClass_FindFunctionByName_SIG = "48 89 54 24 ?? 53 55 56 57 41 55 48 83 EC 40";
        public unsafe delegate UFunction* UClass_FindFunctionByName(UClass* self, FName name, int type); // 0 - ExcludeSuper, 1 - IncludeSuper
        */

        private string FUObjectHashTables_Get_SIG = "E8 ?? ?? ?? ?? 48 8B F8 33 C0 F0 0F B1 35 ?? ?? ?? ??";
        public FUObjectHashTables_Get _getObjectHashTables;
        public unsafe delegate FUObjectHashTables* FUObjectHashTables_Get();

        public GetSpriteItemMaskInstance _getSpriteItemMaskInstance;

        private string UWorld_SpawnActor_SIG = "40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC F8 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ??";
        public UWorld_SpawnActor _spawnActor;
        public unsafe delegate AActor* UWorld_SpawnActor(UWorld* self, UClass* type, FTransform* pUserTransform, FActorSpawnParameters* spawnParams);


        private ObjectMethods __objectMethods;
        private ClassMethods __classMethods;
        public unsafe ClassHooks(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules)
        {
            _context._sharedScans.CreateListener("StaticConstructObject_Internal", addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _staticConstructObject = _context._utils.MakeHooker<StaticConstructObject_Internal>(StaticConstructObject_InternalImpl, addr)));
            _context._sharedScans.CreateListener("GetPrivateStaticClassBody", addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _staticClassBody = _context._utils.MakeHooker<GetPrivateStaticClassBody>(GetPrivateStaticClassBodyImpl, addr)));

            _context._sharedScans.AddScan<UClass_DeferredRegister>(UClass_DeferredRegister_SIG);
            _context._sharedScans.CreateListener<UClass_DeferredRegister>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetDirectAddress, addr => _deferredRegister = _context._utils.MakeWrapper<UClass_DeferredRegister>(addr)));

            _context._sharedScans.AddScan<UObjectProcessRegistrants>(UObjectProcessRegistrants_SIG);
            _context._sharedScans.CreateListener<UObjectProcessRegistrants>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetDirectAddress, addr => _processRegistrants = _context._utils.MakeWrapper<UObjectProcessRegistrants>(addr)));
            // FName::FName(FName* this, wchar_t* Name, EFindFName FindType)
            _context._sharedScans.AddScan<FName_Ctor>(FNameCtor_SIG);
            _context._sharedScans.CreateListener<FName_Ctor>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetDirectAddress, addr => _fnameCtor = _context._utils.MakeWrapper<FName_Ctor>(addr)));
            /*
            _context._sharedScans.AddScan<UClass_FindFunctionByName>(UClass_FindFunctionByName_SIG);
            _context._sharedScans.CreateListener<UClass_FindFunctionByName>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetDirectAddress, addr => _findFunctionByName = _context._utils.MakeWrapper<UClass_FindFunctionByName>(addr)));
            */
            // For Actor Spawning
            _context._sharedScans.CreateListener<GetSpriteItemMaskInstance>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetIndirectAddressShort,
                addr => _getSpriteItemMaskInstance = _context._utils.MakeWrapper<GetSpriteItemMaskInstance>(addr)));
            _context._utils.SigScan(UWorld_SpawnActor_SIG, "UWorld::SpawnActor", _context._utils.GetDirectAddress,
                addr => _spawnActor = _context._utils.MakeWrapper<UWorld_SpawnActor>(addr));
            // Fast object searching
            _context._sharedScans.AddScan<FUObjectHashTables_Get>(FUObjectHashTables_Get_SIG);
            _context._sharedScans.CreateListener<FUObjectHashTables_Get>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetIndirectAddressShort, addr => _getObjectHashTables = _context._utils.MakeWrapper<FUObjectHashTables_Get>(addr)));
        }
        public override void Register()
        {
            // Resolve imports
            __objectMethods = GetModule<ObjectMethods>();
            __classMethods = GetModule<ClassMethods>();
        }

        public unsafe UObject* StaticConstructObject_InternalImpl(FStaticConstructObjectParameters* pParams)
        {
            var newObj = _staticConstructObject.OriginalFunction(pParams);
            if (__objectMethods._objectListeners.TryGetValue(_context.GetObjectType(newObj), out var listeners))
                foreach (var listener in listeners) listener((nint)newObj);
            return newObj;
        }

        // Scuffed Unreal class manipulation

        private unsafe void GetPrivateStaticClassBodyImpl(
            nint packageName,
            nint name,
            UClass** returnClass,
            nint registerNativeFunc,
            uint size,
            uint align,
            uint flags,
            ulong castFlags,
            nint config,
            nint inClassCtor,
            nint vtableHelperCtorCaller, // xor eax,eax, ret
            nint addRefObjects, // ret
            nint superFn, // [superType]::StaticClass
            nint withinFn, // usually UObject::StaticClass
            byte isDynamic,
            nint dynamicFn)
        {
            var className = Marshal.PtrToStringUni(name);
            //_context._utils.Log($"[Constructor] {className}: 0x{inClassCtor:X}");
            //_utils.Log($"Reading class {className}");
            // check if class has been extended, and do appropriate actions
            if (className != null && __classMethods._classNameToClassExtender.TryGetValue(className, out var classExtender))
            {
                // change size
                if (size <= classExtender.Size)
                {
                    _context._utils.Log($"NOTICE: Extended size of class \"{className}\" (from {size} to {classExtender.Size})", LogLevel.Debug);
                    size = classExtender.Size;
                }
                else _context._utils.Log($"ERROR: Class extender for \"{className}\" has defined size smaller than original class (from {size} to {classExtender.Size}). This has been rejected.", System.Drawing.Color.Red, LogLevel.Error);
                // hook ctor
                if (classExtender.CtorHook != null && inClassCtor != 0)
                {
                    var newHook = FollowThunkToGetAppropriateHook(inClassCtor, classExtender.CtorHook);
                    classExtender.CtorHookReal = newHook;
                }
            }
            // add class to static params map - collect info for dynamic class creation
            var packageNameStr = Marshal.PtrToStringUni(packageName);
            /*
            if (className != null && packageNameStr != null)
            {
                __classFactory._classNameToStaticClassParams.TryAdd(className, new StaticClassParams(
                    packageNameStr,
                    className,
                    returnClass,
                    registerNativeFunc,
                    size,
                    align,
                    flags,
                    castFlags,
                    config,
                    inClassCtor,
                    vtableHelperCtorCaller,
                    addRefObjects,
                    superFn,
                    withinFn,
                    isDynamic,
                    dynamicFn
                ));
                
            }
            */
            _staticClassBody.OriginalFunction(packageName, name, returnClass, registerNativeFunc, size, align, flags, castFlags,
                config, inClassCtor, vtableHelperCtorCaller, addRefObjects, superFn, withinFn, isDynamic, dynamicFn);
            if (className != null)
                __classMethods._classNameToType.TryAdd(className, (nint)(*returnClass));
        }
        public unsafe IHook<InternalConstructor> FollowThunkToGetAppropriateHook
            (nint addr, InternalConstructor ctorHook)
        {
            // build a new multicast delegate by injecting the native function, followed by custom code
            // this reference will live for program's lifetime so there's no need to store hook in the caller
            IHook<InternalConstructor>? retHook = null;
            InternalConstructor ctorHookReal = x =>
            {
                if (retHook == null)
                {
                    _context._utils.Log($"ERROR: retHook is null. Game will crash.", System.Drawing.Color.Red, LogLevel.Error);
                    return;
                }
                retHook.OriginalFunction(x);
            };
            ctorHookReal += ctorHook;
            var offset = (int)(addr - (nint)_context._baseAddress);
            var transformed = _context._utils.GetAddressMayThunk(offset);
            //_context._utils.Log($"Transformed address: {addr:X} -> {transformed:X}");
            retHook = _context._utils.MakeHooker(ctorHookReal, (long)transformed).Activate();
            return retHook;
        }
    }

    public class StaticClassParams
    {
        public string PackageName { get; init; }
        public string Name { get; init; }
        public unsafe UClass** Instance { get; init; }
        public nint RegisterNativeFunc { get; init; } // if we want to register functions to blueprints
        public uint Size { get; init; }
        public uint Alignment { get; init; }
        public uint Flags { get; init; }
        public ulong CastFlags { get; init; }
        public nint Config { get; init; }
        public nint InternalConstructor { get; init; }
        public nint VtableHelperCtorCaller { get; init; }
        public nint AddReferantObjects { get; init; }
        public nint SuperStaticClassFn { get; init; }
        public nint BaseStaticClassFn { get; init; } // UObject
        public byte bIsDynamic { get; init; }
        public nint DynamicFn { get; init; }

        public unsafe StaticClassParams(string packageName, string name, UClass** instance, nint registerNativeFunc, uint size, uint alignment, uint flags, ulong castFlags, nint config, nint internalConstructor, nint vtableHelperCtorCaller, nint addReferantObjects, nint superStaticClassFn, nint baseStaticClassFn, byte bIsDynamic, nint dynamicFn)
        {
            PackageName = packageName;
            Name = name;
            Instance = instance;
            RegisterNativeFunc = registerNativeFunc;
            Size = size;
            Alignment = alignment;
            Flags = flags;
            CastFlags = castFlags;
            Config = config;
            InternalConstructor = internalConstructor;
            VtableHelperCtorCaller = vtableHelperCtorCaller;
            AddReferantObjects = addReferantObjects;
            SuperStaticClassFn = superStaticClassFn;
            BaseStaticClassFn = baseStaticClassFn;
            this.bIsDynamic = bIsDynamic;
            DynamicFn = dynamicFn;
        }
    }
}
