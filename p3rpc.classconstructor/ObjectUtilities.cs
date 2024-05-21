using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
namespace p3rpc.classconstructor
{
    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        public string GetFName(FName name) => _context.GetFName(name);
        public unsafe FName GetFName(string name) => GetFNameInner(name, EFindType.FNAME_Find);
        public unsafe FName AddFName(string name) => GetFNameInner(name, EFindType.FNAME_Add);
        private unsafe FName GetFNameInner(string name, EFindType findType)
        {
            // setup params for FName constructor
            FName* newFname = _context._memoryMethods.FMemory_Malloc<FName>((uint)sizeof(nint));
            nint wideName = Marshal.StringToHGlobalUni(name);
            newFname = __classHooks._fnameCtor.Invoke(newFname, wideName, findType);
            __classHooks._fnameCtor.Invoke(newFname, wideName, findType);
            // copy returned fname by value
            FName retFname = *newFname;
            // so we can free it
            Marshal.FreeHGlobal(wideName);
            _context._memoryMethods.FMemory_Free(newFname);
            return retFname;
        }

        public unsafe string GetObjectName(UObject* obj) => _context.GetObjectName(obj);
        public unsafe string GetFullName(UObject* obj) => _context.GetFullName(obj);
        public unsafe string GetObjectType(UObject* obj) => _context.GetObjectType(obj);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type) => _context.IsObjectSubclassOf(obj, type);
        public unsafe bool DoesNameMatch(UObject* tgtObj, string name) => _context.DoesNameMatch(tgtObj, name);
        public unsafe bool DoesClassMatch(UObject* tgtObj, string name) => _context.DoesClassMatch(tgtObj, name);


        // https://github.com/rirurin/Unreal.ObjectDumpToJson/blob/da15d2376084f59ca6b46b990d5ab793673cb3bd/Unreal.ObjectDumpToJson/ObjectDump.cs#L36
        public unsafe void MarkObjectAsRoot(UObject* obj)
        {
            var target_item = &_context.g_objectArray->Objects[obj->InternalIndex >> 0x10][obj->InternalIndex & 0xFFFF];
            target_item->Flags |= EInternalObjectFlags.RootSet;
        }
    }
}