/*! @file       Abstract/OSingleton.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2022-02-17
**/

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

using UnityEngine;
using UnityEngine.SceneManagement;

// TODO remove temporary type spoofs
using SceneRef = UnityEngine.SceneManagement.Scene;


namespace Ore
{


  /// <summary>
  ///   Base class for a singleton object which must exist in scene space;
  ///   it therefore requires a "parent" GameObject and scene context.
  /// </summary>
  /// <typeparam name="TSelf">
  ///   Successor should pass its own type (CRTP).
  /// </typeparam>
  [DisallowMultipleComponent]
  public abstract class OSingleton<TSelf> : OComponent
    where TSelf : OSingleton<TSelf>
  {
    public static TSelf Current => s_Current;
    private static TSelf s_Current;
    public static TSelf Instance => s_Current; // compatibility API


    public static bool IsActive => s_Current && s_Current.isActiveAndEnabled;
    public static bool IsReplaceable => !s_Current || s_Current.m_IsReplaceable;
    public static bool IsDontDestroyOnLoad => s_Current && s_Current.m_DontDestroyOnLoad;


    public static bool FindInstance(out TSelf instance)
    {
      instance = s_Current;
      return instance;
    }



    [Header("Scene Singleton")]

    [SerializeField] // [RequiredReference(DisableIfPrefab = true)]
    protected SceneRef m_OwningScene;

    [SerializeField]
    protected bool m_IsReplaceable;

    [SerializeField]
    protected bool m_DontDestroyOnLoad;

    [SerializeField]
    protected DelayedEvent m_OnFirstInitialized = DelayedEvent.WithApproximateFrameDelay(1);


    [System.NonSerialized]
    private bool m_IsSingletonInitialized;


    [System.Diagnostics.Conditional("DEBUG")]
    public void ValidateInitialization() // good to call as a listener to "On First Initialized"
    {
      // ReSharper disable once HeapView.ObjectAllocation
      OAssert.AllTrue(this, s_Current == this, m_IsSingletonInitialized, isActiveAndEnabled);
      Orator.Log($"Singleton registration validated.", this);
    }


    protected virtual void OnEnable()
    {
      bool ok = TryInitialize((TSelf)this);
      OAssert.True(ok, this);
    }

    protected virtual void OnDisable()
    {
      if (s_Current == this)
        s_Current = null;
    }

    protected virtual void OnValidate()
    {
      m_OwningScene = m_DontDestroyOnLoad ? default : gameObject.scene;
    }


    protected bool TryInitialize(TSelf self)
    {
      if (OAssert.Fails(this == self, $"Proper usage: {nameof(TryInitialize)}(this)", ctx: self))
        return false;

      if (s_Current && s_Current != self)
      {
        if (!s_Current.m_IsReplaceable)
        {
          DestroyGameObject();
          return false;
        }

        s_Current.DestroyGameObject();
      }

      s_Current = self;

      if (m_DontDestroyOnLoad)
        DontDestroyOnLoad(gameObject);

      m_OwningScene = gameObject.scene;

      return m_IsSingletonInitialized || (m_IsSingletonInitialized = m_OnFirstInitialized.TryInvoke());
    }


    protected void SetDontDestroyOnLoad(bool set)
    {
      m_DontDestroyOnLoad = set;

      if (!Application.IsPlaying(this))
        return;

      if (set)
        DontDestroyOnLoad(gameObject);
      else
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

      m_OwningScene = gameObject.scene;
    }

  } // end class OSingleton

}
