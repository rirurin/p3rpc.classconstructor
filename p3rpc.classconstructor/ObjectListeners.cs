using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System.Collections.Concurrent;
namespace p3rpc.classconstructor
{
    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        public ObjectMethods(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) 
        {
            _findObjectThread = new Thread(ProcessObjectQueue);
            _findObjectThread.IsBackground = true;
            _findObjectThread.Start();
        }
        public override void Register() { }
        public ConcurrentDictionary<string, List<Action<nint>>> _objectListeners { get; private init; } = new();

        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => NotifyOnNewObject(_context.GetObjectName((UObject*)type), cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb)
        {
            if (_objectListeners.TryGetValue(typeName, out var listener)) listener.Add(cb);
            else _objectListeners.TryAdd(typeName, new() { cb });
        }
    }
}
