using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System.Collections.Concurrent;
namespace p3rpc.classconstructor
{
    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        public string GetFName(FName name) => _context.GetFName(name);
        public unsafe string GetObjectName(UObject* obj) => _context.GetObjectName(obj);
        public unsafe string GetFullName(UObject* obj) => _context.GetFullName(obj);
        public unsafe string GetObjectType(UObject* obj) => _context.GetObjectType(obj);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type) => _context.IsObjectSubclassOf(obj, type);
        public unsafe bool DoesNameMatch(UObject* tgtObj, string name) => _context.DoesNameMatch(tgtObj, name);
        public unsafe bool DoesClassMatch(UObject* tgtObj, string name) => _context.DoesClassMatch(tgtObj, name);
    }
}