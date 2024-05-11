using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System;
using System.Runtime.InteropServices;
using static p3rpc.nativetypes.Interfaces.IUIMethods;

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

        // O(1) retrieval of a function by name in a given class. Called for each class and it's inherited classes function maps
        private unsafe bool FindFunctionByName(UClass* type, FName name, out UFunction* outFunc)
        {
            var searchFuncByHash = new TMapHashable<FName, nint>((nint)(&type->func_map), 0x40, 0x48);
            outFunc = null;
            UFunction** ppTgtFunc = (UFunction**)searchFuncByHash.TryGetByHash(name);
            if (ppTgtFunc == null) return false;
            outFunc = *ppTgtFunc;
            return true;
        }
        // Get a target function
        public unsafe bool GetFunction(UObject* obj, string name, out UFunction* outFunc)
        {
            UClass* curr_class = obj->ClassPrivate;
            outFunc = null;
            FName targetName = GetFName(name);
            while (curr_class != null)
            {
                if (FindFunctionByName(curr_class, targetName, out outFunc)) return true;
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
        // ProcessEvent (call functions exposed to blueprints)
        private unsafe bool ProcessEventCollectParameters(nint paramAlloc, UFunction* targetFunc, out Dictionary<ProcessEventParameterBase, nint> outNativeProps, params ProcessEventParameterBase[] funcParams)
        {
            outNativeProps = new();
            foreach (var param in funcParams)
            {
                FProperty* curr_prop = FindParameter(targetFunc, param.Name);
                outNativeProps.Add(param, (nint)curr_prop);
                if (curr_prop == null)
                {
                    _context._utils.Log($"ERROR: Function \"{GetObjectName((UObject*)targetFunc)}\" doesn't have a parameter named \"{param.Name}\"", System.Drawing.Color.Red, LogLevel.Error);
                    return false;
                }
                if (param.Size != curr_prop->element_size)
                {
                    _context._utils.Log($"ERROR: Size of value \"{param.Name}\" of type {param.Value.GetType()} doesn't match target property " +
                        $"(got {param.Size}, should be {curr_prop->element_size})", System.Drawing.Color.Red, LogLevel.Error);
                    return false;
                }
                param.AddToAllocation(paramAlloc + curr_prop->offset_internal);
            }
            return true;
        }

        private unsafe void ProcessEventCopyReferenceValues(nint paramAlloc, Dictionary<ProcessEventParameterBase, nint> nativeProps)
        {
            foreach (var nativeProp in nativeProps)
                nativeProp.Key.RetrieveValue(paramAlloc + ((FProperty*)nativeProp.Value)->offset_internal);
        }

        private unsafe nint ProcessEventAllocParameterStorage(UFunction* targetFunc)
        {
            int paramsSize = GetTotalParameterSize(targetFunc);
            nint pParams = _context._memoryMethods.FMemory_Malloc(paramsSize, (uint)sizeof(nint));
            NativeMemory.Fill((void*)pParams, (nuint)paramsSize, 0);
            return pParams;
        }

        public unsafe void ProcessEvent(UObject* obj, string funcName)
        {
            if (!GetFunction(obj, funcName, out UFunction* targetFunc)) return;
            var pParams = ProcessEventAllocParameterStorage(targetFunc);
            var processEventWrapper = _context._utils.MakeWrapper<UObject_ProcessEvent>(*(nint*)(obj->_vtable + 0x220));
            processEventWrapper.Invoke(obj, targetFunc, pParams); // UObject->ProcessEvent (vtable + 0x220 as of UE 4.27)
            _context._memoryMethods.FMemory_Free(pParams);
        }

        public unsafe void ProcessEvent(UObject* obj, string funcName, params ProcessEventParameterBase[] funcParams)
        {
            if (!GetFunction(obj, funcName, out UFunction* targetFunc)) return;
            var pParams = ProcessEventAllocParameterStorage(targetFunc);
            if (ProcessEventCollectParameters(pParams, targetFunc, out var fParams, funcParams))
            {
                var processEventWrapper = _context._utils.MakeWrapper<UObject_ProcessEvent>(*(nint*)(obj->_vtable + 0x220));
                processEventWrapper.Invoke(obj, targetFunc, pParams); // UObject->ProcessEvent
                ProcessEventCopyReferenceValues(pParams, fParams);
            }
            _context._memoryMethods.FMemory_Free(pParams);
        }

        public unsafe TReturnType ProcessEvent<TReturnType>(UObject* obj, string funcName) where TReturnType : unmanaged
        {
            if (!GetFunction(obj, funcName, out UFunction* targetFunc)) return default(TReturnType);
            var pParams = ProcessEventAllocParameterStorage(targetFunc);
            var processEventWrapper = _context._utils.MakeWrapper<UObject_ProcessEvent>(*(nint*)(obj->_vtable + 0x220));
            processEventWrapper.Invoke(obj, targetFunc, pParams);

            FProperty* ret_prop = FindReturnValue(targetFunc);
            TReturnType ret_value = *(TReturnType*)(pParams + ret_prop->offset_internal);
            _context._memoryMethods.FMemory_Free(pParams);
            return ret_value;
        }
        public unsafe TReturnType ProcessEvent<TReturnType>(UObject* obj, string funcName, params ProcessEventParameterBase[] funcParams) where TReturnType : unmanaged
        {
            if (!GetFunction(obj, funcName, out UFunction* targetFunc)) return default(TReturnType);
            var pParams = ProcessEventAllocParameterStorage(targetFunc);
            TReturnType ret_value = default(TReturnType);
            if (ProcessEventCollectParameters(pParams, targetFunc, out var fParams, funcParams))
            {
                var processEventWrapper = _context._utils.MakeWrapper<UObject_ProcessEvent>(*(nint*)(obj->_vtable + 0x220));
                processEventWrapper.Invoke(obj, targetFunc, pParams);
                ProcessEventCopyReferenceValues(pParams, fParams);

                FProperty* ret_prop = FindReturnValue(targetFunc);
                ret_value = *(TReturnType*)(pParams + ret_prop->offset_internal);
            }
            _context._memoryMethods.FMemory_Free(pParams);
            return ret_value;
        }

        public unsafe delegate void UObject_ProcessEvent(UObject* self, UFunction* targetFunc, nint paramData);

        // Actor spawning
        public unsafe AActor* SpawnActor(UClass* type)
        {
            UWorld* currWorld = *(UWorld**)__classHooks._getSpriteItemMaskInstance.Invoke();
            FActorSpawnParameters spawnParams = new();
            FTransform transform = new FTransform();
            spawnParams.objectFlags = 8;
            return __classHooks._spawnActor.Invoke(currWorld, type, &transform, &spawnParams);
        }

        public unsafe AActor* SpawnActor(string type) => SpawnActor(GetType(type));
        public unsafe TActorType* SpawnActor<TActorType>() where TActorType : unmanaged => (TActorType*)SpawnActor(GetType(typeof(TActorType).Name.Substring(1)));

        // Actor components

        // Game subsystem
        public unsafe TSubsystem* GetSubsystem<TSubsystem>(UGameInstance* gameInstance) where TSubsystem : unmanaged
        {
            var subsystemSearch = new TMapHashable<HashablePointer, nint>((nint)(&gameInstance->Subsystems.SubsystemMap), 0x40, 0x48);
            TSubsystem** foundSubsystem = (TSubsystem**)subsystemSearch.TryGetByHash(((nint)GetType(typeof(TSubsystem).Name.Substring(1))).AsHashable());
            return (foundSubsystem != null) ? *foundSubsystem : null;
        }

        public unsafe nint GetSubsystem(UGameInstance* gameInstance, string subsystem)
        {
            var subsystemSearch = new TMapHashable<HashablePointer, nint>((nint)(&gameInstance->Subsystems.SubsystemMap), 0x40, 0x48);
            var foundSubsystem = subsystemSearch.TryGetByHash(((nint)GetType(subsystem)).AsHashable());
            return (foundSubsystem != null) ? *foundSubsystem : nint.Zero;
        }

        // Fast object searching
        public unsafe UObject* FindObjectFast(string objName, UObject* outer, string objClass)
        {
            FUObjectHashTables* hashTables = __classHooks._getObjectHashTables.Invoke();
            var targetObjectHash = (int)(GetFName(objName).GetTypeHash() + ((nint)outer >> 6));
            var outerHashesCheck = new TMapHashable<HashableInt, nint>((nint)(&hashTables->HashesOuter), 0x40, 0x48);
            var resultObject = (UObject**)outerHashesCheck.TryGetByHash(targetObjectHash.AsHashable());
            if (resultObject != null)
            {
                var foundObject = *resultObject;
                UClass* targetType = GetType(objClass);
                if (foundObject->NamePrivate.Equals(GetFName(objName)) 
                    && foundObject->ClassPrivate == targetType)
                    return foundObject;
            }
            return null;
        }
        public unsafe TObjectType* FindObjectFast<TObjectType>(string objName, UObject* outer) where TObjectType : unmanaged
            => (TObjectType*)FindObjectFast(objName, outer, typeof(TObjectType).Name.Substring(1));
    }
}
