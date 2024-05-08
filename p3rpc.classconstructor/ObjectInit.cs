using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using System;
using System.Collections.Concurrent;

namespace p3rpc.classconstructor
{
    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        private ClassHooks __classHooks;
        private ClassMethods __classMethods;

        public ConcurrentDictionary<string, List<Action<nint>>> _objectListeners { get; private init; } = new();
        private Thread _findObjectThread { get; init; }
        private BlockingCollection<FindObjectBase> _findObjects { get; init; } = new();
        public ObjectMethods(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules)
        {
            _findObjectThread = new Thread(ProcessObjectQueue);
            _findObjectThread.IsBackground = true;
            _findObjectThread.Start();
        }
        public override void Register() 
        {
            __classHooks = GetModule<ClassHooks>();
            __classMethods = GetModule<ClassMethods>();
        }
    }
}
