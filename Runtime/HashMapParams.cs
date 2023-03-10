/*! @file       Runtime/HashMapParams.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2022-09-28
 *
 *  A parameter POD storing and modifying performance-critical HashMap
 *  configurations.
**/

using JetBrains.Annotations;

using UnityEngine;

#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif


namespace Ore
{
  #if NEWTONSOFT_JSON
  [JsonObject(MemberSerialization.Fields)]
  #endif
  [System.Serializable]
  public struct HashMapParams
  {

  #region Constants + Defaults

    [System.Flags]
    public enum Policies
    {
      Default = HashProbing,

      NotImplemented   = (1 << 31),

      HashProbing      = (1 << 0), // == double hashing, where the 2nd hash func == the 1st
      LinearProbing    = (1 << 1) | NotImplemented,
      QuadraticProbing = (1 << 2) | NotImplemented,
      RobinHoodHashing = (1 << 3) | NotImplemented,
      HopscotchHashing = (1 << 4) | NotImplemented,
      CuckooHashing    = (1 << 5) | NotImplemented,

      SeparateChaining = (1 << 6) | NotImplemented,
      ParallelChaining = (1 << 7) | NotImplemented,
    }

    private const int USERCAPACITY_DEFAULT = 5;

    private const int HASHPRIME_DEFAULT    = Hashing.DefaultHashPrime;

    private const int INTERNALSIZE_MIN     = Primes.MinValue;
    private const int INTERNALSIZE_MAX     = Primes.MaxSizePrime;

    private const float LOADFACTOR_DEFAULT = 0.72f;
    private const float LOADFACTOR_MIN     = 0.1f * LOADFACTOR_DEFAULT;
    private const float LOADFACTOR_MAX     = 1f;    // danger

    private const float GROWFACTOR_MIN     = 1.05f;

    private static readonly AnimationCurve GROWTHCURVE_DEFAULT
      = AnimationCurve.Constant(0f, 0f, 2f);

    private const bool UNMAP_NULLS_DEFAULT = false;


    [PublicAPI]
    public static readonly HashMapParams Default = new HashMapParams(USERCAPACITY_DEFAULT);

  #endregion Constants + Defaults


  #region Properties + Fields

    public bool IsFixedSize => m_GrowthCurve is null;


    [SerializeField, Range(INTERNALSIZE_MIN, INTERNALSIZE_MAX)]
    public int InitialSize;

    [SerializeField, Range(LOADFACTOR_MIN, LOADFACTOR_MAX)]
    public float LoadFactor;

    [SerializeField, Min(Primes.MinValue)]
    public int HashPrime;

    [SerializeField]
    public Policies Policy;


    // private due to class reference danger:
    [SerializeField]
    private AnimationCurve m_GrowthCurve;

  #endregion Properties + Fields


  #region Constructors + Factory Funcs

    [PublicAPI]
    public static HashMapParams FixedCapacity(int userCapacity, float loadFactor = LOADFACTOR_DEFAULT)
    {
      return new HashMapParams(
        initialCapacity: userCapacity,
        isFixed:         true,
        loadFactor:      loadFactor,
        hashPrime:       HASHPRIME_DEFAULT);
    }

    /// <summary>
    /// You should only need to instantiate this struct if you're trying to play
    /// around with or optimize a particular HashMap instance.
    /// </summary>
    /// <param name="initialCapacity">
    /// Interpreted as initial "user" capacity, not physical capacity.
    /// </param>
    /// <param name="hashPrime">
    /// You should (probably) select a pre-designated hashprime, such as from
    /// the <see cref="Hashing"/> class. If a non-prime number is given, the
    /// nearest prime number will be used instead.
    /// </param>
    /// <param name="policy">
    /// Choices other than the default are not yet implemented.
    /// </param>
    [PublicAPI]
    public HashMapParams(
      int      initialCapacity,
      float    loadFactor = LOADFACTOR_DEFAULT,
      bool     isFixed    = false,
      int      hashPrime  = HASHPRIME_DEFAULT,
      Policies policy     = Policies.Default)
    {
      LoadFactor = loadFactor.Clamp(LOADFACTOR_MIN, LOADFACTOR_MAX);

      HashPrime = Primes.NearestTo(hashPrime & int.MaxValue);

      InitialSize = Primes.NextHashableSize((int)(initialCapacity / LoadFactor), HashPrime, 0);

      Policy = policy;

      m_GrowthCurve = isFixed ? null : GROWTHCURVE_DEFAULT;
    }

    [PublicAPI]
    public HashMapParams(Policies policies)
      : this(USERCAPACITY_DEFAULT, policy: policies)
    {
    }

    public HashMapParams WithGrowthCurve([CanBeNull] AnimationCurve growthCurve)
    {
      m_GrowthCurve = growthCurve;
      return this;
    }

    public HashMapParams WithGrowFactor(float factor, int atSize)
    {
      if (m_GrowthCurve is null)
      {
        m_GrowthCurve = new AnimationCurve(new Keyframe(atSize, factor));
      }
      else
      {
        _ = m_GrowthCurve.AddKey(atSize, factor);
      }

      return this;
    }

  #endregion Constructors + Factory Funcs


  #region Methods

    [Pure]
    public bool Check()
    {
      #if UNITY_EDITOR || DEBUG || UNITY_INCLUDE_TESTS
      // ReSharper is saying the next line of code is always true, but I disagree.
      // Just consider if this struct was default constructed, and InitialSize = 0...
      return  (INTERNALSIZE_MIN <= InitialSize && InitialSize <= INTERNALSIZE_MAX) &&
              (LOADFACTOR_MIN <= LoadFactor && LoadFactor <= LOADFACTOR_MAX)       &&
              (HashPrime == HASHPRIME_DEFAULT || Primes.IsPrime(HashPrime))        &&
              (InitialSize == 7 || Primes.IsPrime(InitialSize));
      #else
      return InitialSize > 0 && HashPrime > Primes.MinValue;
      #endif
    }

    public void ResetInitialSize()
    {
      InitialSize = CalcInternalSize(USERCAPACITY_DEFAULT);
    }

    public int StoreLoadLimit(int loadLimit)
    {
      return InitialSize = CalcInternalSize(loadLimit);
    }


    [Pure]
    public int CalcInternalSize(int loadLimit)
    {
      return Primes.NextHashableSize((int)(loadLimit / LoadFactor), HashPrime, 0);
    }

    [Pure]
    public int CalcLoadLimit(int internalSize) // AKA User Capacity
    {
      // without the rounding, we get wonky EnsureCapacity behavior
      return (int)(internalSize * LoadFactor + 0.5f);
    }

    [Pure]
    public int CalcLoadLimit()
    {
      return (int)(InitialSize * LoadFactor + 0.5f);
    }

    [Pure]
    public int CalcJump(int hash31, int size)
    {
      return 1 + (hash31 * HashPrime & int.MaxValue) % (size - 1);
    }

    [Pure]
    public int CalcNextSize(int prevSize, int maxSize = Primes.MaxValue)
    {
      if (m_GrowthCurve is null)
        return prevSize;

      float growFactor = m_GrowthCurve.Evaluate(prevSize);
      if (growFactor < GROWFACTOR_MIN)
        return prevSize;

      if ((int)(maxSize / growFactor) < prevSize)
        return maxSize;

      return Primes.NextHashableSize((int)(prevSize * growFactor), HashPrime);
    }

  #endregion Methods


    public static implicit operator HashMapParams (int initialCapacity)
    {
      return new HashMapParams(initialCapacity);
    }

  } // end class HashMapParams
}