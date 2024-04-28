﻿#pragma warning disable CS1591
using p3rpc.nativetypes.Interfaces;

namespace p3rpc.classconstructor.Interfaces;
public interface IObjectMethods
{
    // Object Listeners
    public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb);
    public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb);
    // Object Search
    public unsafe UObject* FindObject(string targetObj, string? objType = null);
    public unsafe ICollection<nint> FindAllObjectsNamed(string targetObj, string? objType = null);
    public unsafe UObject* FindFirstOf(string objType);
    public unsafe ICollection<nint> FindAllOf(string objType);
    public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb);
    public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb);
    public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb);
    public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb);
    public unsafe UObject* GetEngineTransient();
    public unsafe UClass* GetType(string type);
    public unsafe void GetTypeAsync(string type, Action<nint> foundCb);
    // Object Utilities
    public string GetFName(FName name);
    public unsafe string GetObjectName(UObject* obj);
    public unsafe string GetFullName(UObject* obj);
    public unsafe string GetObjectType(UObject* obj);
    public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type);
    public unsafe bool DoesNameMatch(UObject* tgtObj, string name);
    public unsafe bool DoesClassMatch(UObject* tgtObj, string name);
}