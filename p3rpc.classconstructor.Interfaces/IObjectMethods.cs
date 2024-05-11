#pragma warning disable CS1591
using p3rpc.nativetypes.Interfaces;

namespace p3rpc.classconstructor.Interfaces;

public interface IObjectMethods
{
    // Object Listeners
    public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb);
    public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb);
    public unsafe void NotifyOnNewObject<TNotifyType>(Action<nint> cb) where TNotifyType : unmanaged;
    // Object Search
    public unsafe UObject* FindObject(string targetObj, string? objType = null);
    public unsafe TObjectType* FindObject<TObjectType>(string targetObj) where TObjectType : unmanaged;
    public unsafe ICollection<nint> FindAllObjectsNamed(string targetObj, string? objType = null);
    public unsafe UObject* FindFirstOf(string objType);
    public unsafe TObjectType* FindFirstOf<TObjectType>() where TObjectType : unmanaged;
    public unsafe ICollection<nint> FindAllOf(string objType);
    public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb);
    public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb);
    public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb);
    public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb);
    public unsafe UObject* FindFirstSubclassOf(string objType);
    public unsafe UObject* FindFirstSubclassOf(UClass* super);
    public unsafe UObject* GetEngineTransient();
    public unsafe UClass* GetType(string type);
    public unsafe void GetTypeAsync(string type, Action<nint> foundCb);

    // Fast Object search
    public unsafe UObject* FindObjectFast(string objName, UObject* outer, string objClass);
    public unsafe TObjectType* FindObjectFast<TObjectType>(string objName, UObject* outer) where TObjectType : unmanaged;

    // Object Utilities
    public string GetFName(FName name);
    public unsafe FName GetFName(string name);
    public unsafe FName AddFName(string name);
    public unsafe string GetObjectName(UObject* obj);
    public unsafe string GetFullName(UObject* obj);
    public unsafe string GetObjectType(UObject* obj);
    public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type);
    public unsafe bool DoesNameMatch(UObject* tgtObj, string name);
    public unsafe bool DoesClassMatch(UObject* tgtObj, string name);
    // Object Spawning
    public unsafe UObject* SpawnObject(UClass* type, UObject* owner, FName? name = null);
    public unsafe UObject* SpawnObject(string type, UObject* owner, FName? name = null);
    public unsafe UObject* SpawnObject(string type, UObject* owner, string name);
    // Invoke Methods
    public unsafe bool GetFunction(UObject* obj, string name, out UFunction* outFunc);
    public unsafe void ProcessEvent(UObject* obj, string funcName);
    public unsafe void ProcessEvent(UObject* obj, string funcName, params ProcessEventParameterBase[] funcParams);
    public unsafe TReturnType ProcessEvent<TReturnType>(UObject* obj, string funcName) where TReturnType : unmanaged;
    public unsafe TReturnType ProcessEvent<TReturnType>(UObject* obj, string funcName, params ProcessEventParameterBase[] funcParams) where TReturnType : unmanaged;

    // Actor Spawning

    public unsafe AActor* SpawnActor(UClass* type);
    public unsafe AActor* SpawnActor(string type);
    public unsafe TActorType* SpawnActor<TActorType>() where TActorType : unmanaged;

    // Actor Components

    // Game Subsystems
    public unsafe nint GetSubsystem(UGameInstance* gameInstance, string subsystem);
    public unsafe TSubsystem* GetSubsystem<TSubsystem>(UGameInstance* gameInstance) where TSubsystem : unmanaged;

}