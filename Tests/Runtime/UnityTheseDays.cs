/*! @file       Tests/Runtime/UnityTheseDays.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2023-01-27
**/

using Ore;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Profiling;


// ReSharper disable once CheckNamespace
public static class UnityTheseDays
{

  [Test]
  public static void AllocatesThisMuchForNewGameObject()
  {
    // System.GC.Collect();

    long managed = Profiler.GetMonoUsedSizeLong();

    var go = new GameObject("Empty");

    managed = Profiler.GetMonoUsedSizeLong() - managed;
    Debug.Log($"managed mem: {managed} bytes"); // 0 for some reason

    long native = Profiler.GetRuntimeMemorySizeLong(go);

    foreach (var comp in go.GetComponentsInChildren<Component>(includeInactive: true))
    {
      long compSize = Profiler.GetRuntimeMemorySizeLong(comp);
      Debug.Log($"{comp.name}.{comp.GetType().Name}: {compSize} bytes");
      native += compSize;
    }

    Debug.Log($"native mem: {native} bytes");

    Debug.Log($"total mem: {managed + native} bytes");

    Assert.Positive(managed + native, "total memory usage");
  }

  [Test]
  public static void AllocatesThisMuchForActiveSceneObject()
  {
    // System.GC.Collect();

    long managed = Profiler.GetMonoUsedSizeLong();

    var go = ActiveScene.Current.gameObject;

    managed = Profiler.GetMonoUsedSizeLong() - managed;
    Debug.Log($"managed mem: {managed} bytes");

    long native = Profiler.GetRuntimeMemorySizeLong(go);

    foreach (var comp in go.GetComponentsInChildren<Component>(includeInactive: true))
    {
      long compSize = Profiler.GetRuntimeMemorySizeLong(comp);
      Debug.Log($"{comp.name}.{comp.GetType().Name}: {compSize} bytes");
      native += compSize;
    }

    Debug.Log($"native mem: {native} bytes");

    Debug.Log($"total mem: {managed + native} bytes");

    Assert.Positive(managed + native, "total memory usage");
  }

}