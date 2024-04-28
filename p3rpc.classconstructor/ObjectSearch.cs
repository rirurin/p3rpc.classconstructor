using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System.Collections.Concurrent;
namespace p3rpc.classconstructor
{
    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        private Thread _findObjectThread { get; init; }
        private BlockingCollection<FindObjectBase> _findObjects { get; init; } = new();
        public abstract class FindObjectBase
        {
            protected ObjectMethods Context { get; init; }
            public FindObjectBase(ObjectMethods context) { Context = context; }
            public abstract void Execute();
        }
        public class FindObjectByName : FindObjectBase
        {
            public string ObjectName { get; set; }
            public string? TypeName { get; set; }
            public Action<nint> FoundObjectCallback { get; set; } // Action<UObject*>
            public FindObjectByName(ObjectMethods context, string objectName, string? typeName, Action<nint> foundCb)
                : base(context)
            {
                ObjectName = objectName;
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindObject(ObjectName, TypeName);
                if (foundObj != null) FoundObjectCallback((nint)foundObj);
            }
        }
        public class FindObjectFirstOfType : FindObjectBase
        {
            public string TypeName { get; set; }
            public Action<nint> FoundObjectCallback { get; set; }
            public FindObjectFirstOfType(ObjectMethods context, string typeName, Action<nint> foundCb)
                : base(context)
            {
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindFirstOf(TypeName);
                if (foundObj != null) FoundObjectCallback((nint)foundObj);
            }
        }
        public class FindObjectAllOfType : FindObjectBase
        {
            public string TypeName { get; set; }
            public Action<ICollection<nint>> FoundObjectCallback { get; set; }
            public FindObjectAllOfType(ObjectMethods context, string typeName, Action<ICollection<nint>> foundCb)
                : base(context)
            {
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindAllOf(TypeName);
                if (foundObj != null) FoundObjectCallback(foundObj);
            }
        }

        private unsafe void ForEachObject(Action<nint> objItem)
        {
            for (int i = 0; i < _context.g_objectArray->NumElements; i++)
            {
                var currObj = &_context.g_objectArray->Objects[i >> 0x10][i & 0xffff];
                if (currObj->Object == null || (currObj->Flags & EInternalObjectFlags.Unreachable) != 0) continue;
                objItem((nint)currObj);
            }
        }

        // Synchronous operations for finding objects. This will block the caller thread for a while
        public unsafe UObject* FindObject(string targetObj, string? objType = null)
        {
            UObject* ret = null;
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesNameMatch(currObj->Object, targetObj))
                {
                    if (objType == null || _context.DoesClassMatch(currObj->Object, objType))
                    {
                        ret = currObj->Object;
                        return;
                    }
                }
            });
            return ret;
        }
        public unsafe ICollection<nint> FindAllObjectsNamed(string targetObj, string? objType = null)
        {
            var objects = new List<nint>();
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesNameMatch(currObj->Object, targetObj))
                {
                    if (objType == null || _context.DoesClassMatch(currObj->Object, objType))
                        objects.Add((nint)currObj->Object);
                }
            });
            return objects;
        }
        public unsafe UObject* FindFirstOf(string objType)
        {
            UObject* ret = null;
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesClassMatch(currObj->Object, objType))
                {
                    ret = currObj->Object;
                    return;
                }
            });
            return ret;
        }
        public unsafe ICollection<nint> FindAllOf(string objType)
        {
            var objects = new List<nint>();
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesClassMatch(currObj->Object, objType))
                    objects.Add((nint)currObj->Object);
            });
            return objects;
        }

        public unsafe UObject* FindFirstSubclassOf(string objType)
        {
            UObject* ret = null;
            UClass* super = GetType(objType);
            return FindFirstSubclassOf(super);
        }
        public unsafe UObject* FindFirstSubclassOf(UClass* super)
        {
            UObject* ret = null;
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.IsObjectDirectSubclassOf(currObj->Object, super))
                {
                    ret = currObj->Object;
                    return;
                }
            });
            return ret;
        }

        // Async object finding operations
        public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb) => _findObjects.Add(new FindObjectByName(this, targetObj, objType, foundCb));
        public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb) => FindObjectAsync(targetObj, null, foundCb);
        public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb) => _findObjects.Add(new FindObjectFirstOfType(this, objType, foundCb));
        public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb) => _findObjects.Add(new FindObjectAllOfType(this, objType, foundCb));

        private void ProcessObjectQueue()
        {
            try
            {
                while (true)
                {
                    var findObjectRequest = _findObjects.Take();
                    findObjectRequest.Execute();
                }
            }
            catch (OperationCanceledException) { } // Called during process termination
        }

        public unsafe UObject* GetEngineTransient() => FindObject("/Engine/Transient", "Package");
        public unsafe UClass* GetType(string type) => (UClass*)FindObject(type, "Class");
        public unsafe void GetTypeAsync(string type, Action<nint> foundCb) => FindObjectAsync(type, "Class", foundCb);
    }
}
