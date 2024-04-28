using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using SharedScans.Interfaces;

namespace p3rpc.classconstructor
{
    internal class ClassConstructorContext : Context
    {
        // Imports from Unreal Engine
        public unsafe FNamePool* g_namePool { get; private set; }
        public unsafe FUObjectArray* g_objectArray { get; private set; }
        public IMemoryMethods _memoryMethods { get; private set; }

        private nuint TransformAddressForFUObjectArray(int offset) => Utils.GetGlobalAddress((nint)(_baseAddress + offset + 3)) - 0x10;
        public ClassConstructorContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner,
            IReloadedHooks hooks, string modLocation, Utils utils, Memory memory, ISharedScans sharedScans, IMemoryMethods memoryMethods)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans)
        {
            unsafe
            {
                _sharedScans.CreateListener("FUObjectArray", addr => _utils.AfterSigScan(addr, TransformAddressForFUObjectArray, addr => g_objectArray = (FUObjectArray*)addr));
                _sharedScans.CreateListener("FGlobalNamePool", addr => _utils.AfterSigScan(addr, _utils.GetIndirectAddressLong, addr => g_namePool = (FNamePool*)addr));
            }
            _memoryMethods = memoryMethods;
        }

        // Utility functions

        public unsafe string GetFName(FName name) => g_namePool->GetString(name);
        public unsafe string GetObjectName(UObject* obj) => g_namePool->GetString(obj->NamePrivate);
        private unsafe string GetPathName(UObject* obj, UObject* end)
        {
            var path = GetObjectName(obj);
            if (obj->OuterPrivate != null)
            {
                var separator = obj == end ? ":" : ".";
                path = $"{GetPathName(obj->OuterPrivate, end)}{separator}{path}";
            }
            return path;
        }
        public unsafe string GetFullName(UObject* obj) // path name used throughout UE4SS
        {
            return obj->OuterPrivate != null ? GetPathName(obj, obj) : GetObjectName(obj);
        }
        public unsafe string GetObjectType(UObject* obj) => g_namePool->GetString(((UObject*)obj->ClassPrivate)->NamePrivate);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type)
        {
            var currType = obj->ClassPrivate;
            while (currType != null)
            {
                if (g_namePool->GetString(((UObject*)currType)->NamePrivate).Equals("Object")) break; // UObject is base type
                if (((UObject*)currType)->NamePrivate.Equals(((UObject*)type)->NamePrivate))
                    return true;
                currType = (UClass*)currType->_super.super_struct;
            }
            return false;
        }
        public unsafe bool IsObjectDirectSubclassOf(UObject* obj, UClass* type)
        {
            var currType = obj->ClassPrivate;
            if (GetObjectName((UObject*)currType).Equals("Object")) return false; // UObject is base class, can't get superclass of that
            var superType = (UClass*)currType->_super.super_struct;
            if (((UObject*)superType)->NamePrivate.Equals(((UObject*)type)->NamePrivate)) return true;
            return false;
        }
        public unsafe bool DoesNameMatch(UObject* tgtObj, string name) => g_namePool->GetString(tgtObj->NamePrivate).Equals(name);
        public unsafe bool DoesClassMatch(UObject* tgtObj, string name) => g_namePool->GetString(((UObject*)tgtObj->ClassPrivate)->NamePrivate).Equals(name);
    }
}
