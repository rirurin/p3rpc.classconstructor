using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace p3rpc.classconstructor
{
    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        // Object Spawning
        public unsafe UObject* SpawnObject(UClass* type, UObject* owner, FName? name = null)
        {
            var objParams = (FStaticConstructObjectParameters*)NativeMemory.AllocZeroed((nuint)sizeof(FStaticConstructObjectParameters));
            objParams->Class = type;
            objParams->Outer = owner;
            if (name != null) objParams->Name = name.Value;
            var newObj = __classHooks.StaticConstructObject_InternalImpl(objParams);
            NativeMemory.Free(objParams);
            return newObj;
        }
        public unsafe UObject* SpawnObject(string type, UObject* owner, FName? name = null)
        {
            if (__classMethods._classNameToType.TryGetValue(type, out var typePtr))
                return SpawnObject((UClass*)typePtr, owner, name);
            return null;
        }

        public unsafe UObject* SpawnObject(string type, UObject* owner, string name)
        {
            if (__classMethods._classNameToType.TryGetValue(type, out var typePtr))
                return SpawnObject((UClass*)typePtr, owner, AddFName(name));
            return null;
        }

        // ProcessEvent (call functions exposed to blueprints)
        // GetFunctionUsingHook
        public unsafe bool GetFunction(UObject* obj, string name, out UFunction* outFunc)
        {
            outFunc = __classHooks._findFunctionByName.Invoke(obj->ClassPrivate, GetFName(name), 1);
            return outFunc != null;
        }
        // GetFunction
        private unsafe bool FindFunctionByName(UClass* type, FName name, out UFunction* outFunc)
        {
            TMap<FName, nint> funcs = type->func_map; // TMap<FName, UFunction*>
            outFunc = null;
            for (int i = 0; i < funcs.mapNum; i++)
            {
                UFunction* currFunc = *(UFunction**)funcs.GetByIndex(i);
                if (((UObject*)currFunc)->NamePrivate.Equals(name))
                    outFunc = currFunc;
            }
            return outFunc != null;
        }

        private unsafe bool FindFunctionByNameEx(UClass* type, FName name, out UFunction* outFunc)
        {
            var searchFuncByHash = new TMapHashable<FName, nint>((nint)(&type->func_map), 0x40, 0x48);
            outFunc = *(UFunction**)searchFuncByHash.TryGetByHash(name);
            return outFunc != null;
        }
        public unsafe bool GetFunctionEx(UObject* obj, string name, out UFunction* outFunc)
        {
            UClass* curr_class = obj->ClassPrivate;
            outFunc = null;
            FName targetName = GetFName(name);
            while (curr_class != null)
            {
                if (FindFunctionByNameEx(curr_class, targetName, out outFunc)) return true;
                else curr_class = (UClass*)((UStruct*)curr_class)->super_struct;
            }
            return false;
        }

        private unsafe void ForEachProperty(UFunction* func, Action<nint> propCb)
        {
            // https://github.com/rirurin/Unreal.ObjectDumpToJson/blob/master/Unreal.ObjectDumpToJson/ObjectDump.cs#L234
            FProperty* curr_param = (FProperty*)((UStruct*)func)->child_properties;
            while (curr_param != null)
            {
                propCb((nint)curr_param);
                curr_param = (FProperty*)curr_param->_super.next;
            }
        }

        private unsafe int GetTotalParameterSize(UFunction* func)
        {
            int paramSize = 0;
            ForEachProperty(func, pProp => paramSize += ((FProperty*)pProp)->element_size);
            return paramSize;
        }
        private unsafe FProperty* FindReturnValue(UFunction* func)
        {
            FProperty* ret = null;
            ForEachProperty(func, pProp =>
            {
                FProperty* curr_prop = (FProperty*)pProp;
                if ((curr_prop->property_flags & EPropertyFlags.CPF_ReturnParm) != 0)
                {
                    ret = curr_prop;
                    return;
                }
            });
            return ret;
        }

        private unsafe FProperty* FindParameter(UFunction* func, string name)
        {
            FProperty* ret = null;
            FName tgtName = GetFName(name);
            ForEachProperty(func, pProp =>
            {
                FProperty* curr_prop = (FProperty*)pProp;
                if ((curr_prop->property_flags & EPropertyFlags.CPF_ReturnParm) == 0
                && ((FField*)curr_prop)->name_private.pool_location == tgtName.pool_location)
                {
                    ret = curr_prop;
                    return;
                }
            });
            return ret;
        }

        // Call functions with one or more params and no return value
        public unsafe void ProcessEvent(UObject* obj, string funcName, params (string name, nint value, nint size)[] funcParams)
        {
            if (!GetFunction(obj, funcName, out UFunction* targetFunc)) return;
            int paramsSize = GetTotalParameterSize(targetFunc);
            nint pParams = _context._memoryMethods.FMemory_Malloc(paramsSize, (uint)sizeof(nint));
            // build param data
            foreach (var param in funcParams)
            {
                FProperty* curr_prop = FindParameter(targetFunc, param.name);
                if (param.size != curr_prop->element_size)
                {
                    _context._utils.Log($"ERROR: Size of value {param.name} of type {param.value.GetType()} doesn't match target property " +
                        $"(got {param.size}, should be {curr_prop->element_size})", System.Drawing.Color.Red, LogLevel.Error);
                }
                nint paramStoreTmp = _context._memoryMethods.FMemory_Malloc(param.size, (uint)sizeof(nint));
                NativeMemory.Copy((void*)paramStoreTmp, (void*)(pParams + curr_prop->offset_internal), (nuint)param.size);
                _context._memoryMethods.FMemory_Free(paramStoreTmp);
            }
            // call UObject->ProcessEvent
            var processEventWrapper = _context._utils.MakeWrapper<UObject_ProcessEvent>(*(nint*)(obj->_vtable + 0x220));
            processEventWrapper.Invoke(obj, targetFunc, pParams);
            _context._memoryMethods.FMemory_Free(pParams);
        }
        // Call functions with no params or return value
        /*
        public unsafe void ProcessEvent(UObject* obj, string funcName)
        {
            if (!GetFunction(obj, funcName, out UFunction* targetFunc)) return;
            int paramsSize = GetTotalParameterSize(targetFunc);
            nint pParams = _context._memoryMethods.FMemory_Malloc(paramsSize, (uint)sizeof(nint));
            var processEventWrapper = _context._utils.MakeWrapper<UObject_ProcessEvent>(*(nint*)(obj->_vtable + 0x220));
            processEventWrapper.Invoke(obj, targetFunc, pParams);
            _context._memoryMethods.FMemory_Free(pParams);
        }
        // Call functions with one or more params and a return value
        public unsafe T ProcessEvent<T>(UObject* obj, string funcName, params (string name, nint value, nint size)[] funcParams)
            where T : unmanaged => default(T);
        */

        public unsafe delegate void UObject_ProcessEvent(UObject* self, UFunction* targetFunc, nint paramData);
    }
}
