using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System.Collections.Concurrent;
namespace p3rpc.classconstructor
{
    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => NotifyOnNewObject(_context.GetObjectName((UObject*)type), cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb)
        {
            if (_objectListeners.TryGetValue(typeName, out var listener)) listener.Add(cb);
            else _objectListeners.TryAdd(typeName, new() { cb });
        }
        public unsafe void NotifyOnNewObject<TNotifyType>(Action<nint> cb) where TNotifyType : unmanaged
            => NotifyOnNewObject(typeof(TNotifyType).Name.Substring(1), cb);
    }
}
