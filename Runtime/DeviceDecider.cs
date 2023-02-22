/*! @file     Runtime/DeviceDecider.cs
 *  @author   Levi Perez (levi\@leviperez.dev)
 *  @date     2022-06-01
 *
 *  @details  Here's a complete example of how to use this class:
```csharp
private class ExampleUsage_DeviceDecider
{

  [SerializeField] // NOTICE: this doesn't need to be a serialized field if coming from flights!
  private SerialDeviceDecider m_DeviceDeciderRawData;
    // (...however, it being serialized here means it's super easy to view values in the Inspector.)


  [System.NonSerialized]
  private bool? m_CachedDecision = null; // optional optimization suggestion.

  private bool GetDecision()
  {
    if (m_CachedDecision != null)
      return (bool)m_CachedDecision;

    var decider = new DeviceDecider(m_DeviceDeciderRawData);
      // ( you can also use `TryDeserialize(...)` or `TryParseRow(...)`
      //   instead of this constructor to implement more defensive coding! )

    // you can use either this form, probably for extra logging:
    bool decision = decider.Decide(out float total_weight);
    Debug.Log($"decision={decision}; total_weight={total_weight}");

    // or you can use this form, which can short-circuit to be slightly faster at runtime.
    bool decision_fast = decider.DecideShort();

    Debug.Assert(decision == decision_fast, "decision == decision_fast");

    m_CachedDecision = decision;
    return decision;
  }
}
```
**/

using System.Collections.Generic;

using UnityEngine;


namespace Ore
{
  public class DeviceDecider
  {
    public float Threshold
    {
      get => m_Threshold;
      set => m_Threshold = value;
    }
    public bool Verbose
    {
      get => m_Verbose;
      set => m_Verbose = value;
    }


    private static readonly char[] KEY_SEPARATORS = new char[] { '|' };


    public const float MAX_THRESHOLD = float.MaxValue / 2f;

    private float m_Threshold = DEFAULT_THRESHOLD;
    private const float DEFAULT_THRESHOLD = 1f;

    private readonly HashMap<DeviceDimension,DeviceEvaluator> m_Factors
      = new HashMap<DeviceDimension,DeviceEvaluator>();

    private bool m_Verbose = false;


    public DeviceDecider()
    {
    }
    public DeviceDecider(SerialDeviceDecider sdd)
    {
      _ = TryDeserialize(sdd);
    }


    public void Clear()
    {
      m_Factors.Clear();
    }


    public bool TryDeserialize(SerialDeviceDecider sdd)
    {
      if (sdd.Rows == null)
        return false;

      int count = 0;

      foreach (var row in sdd.Rows)
      {
        TryParseRow(row.Dimension, row.Key, row.Weight);
        ++ count;
      }

      return count > 0;
    }

    public bool TryParseRow(string dimension, string key, string weight)
    {
      if (float.TryParse(weight, out float w) &&
          DeviceDimensions.TryParse(dimension, out DeviceDimension dim))
      {
        if (!m_Factors.TryGetValue(dim, out DeviceEvaluator evaluator))
        {
          evaluator = new DeviceEvaluator(dim);
          m_Factors[dim] = evaluator;
        }

        var splits = key.Split(KEY_SEPARATORS, System.StringSplitOptions.RemoveEmptyEntries);

        if (splits.IsEmpty())
          return false;

        foreach (var split in splits)
        {
          evaluator.Key(split.Trim(), w);
        }

        return true;
      }

      return false;
    }


    public void EaseCurves()
    {
      foreach (var factor in GetContinuousFactors())
      {
        factor.EaseCurve();
      }
    }

    public void SmoothCurves(float weight = 1f)
    {
      foreach (var factor in GetContinuousFactors())
      {
        factor.SmoothCurve(weight);
      }
    }


    public bool IsDisabled(out bool decision)
    {
      decision = m_Threshold < MAX_THRESHOLD;
      return m_Threshold < Floats.Epsilon || m_Threshold >= MAX_THRESHOLD;
    }

    public bool IsDisabled()
    {
      return m_Threshold < Floats.Epsilon || m_Threshold >= MAX_THRESHOLD;
    }


    public bool Decide()
    {
      return Decide(out _ );
    }

    public bool Decide(out float sum)
    {
      // this version doesn't short-circuit.

      sum = 0f;

      if (IsDisabled(out bool decision))
      {
        if (m_Verbose)
        {
          Debug.Log($"<{nameof(DeviceDecider)}>  is disabled. Default decision: {decision}");
        }

        return decision;
      }

      if (m_Verbose)
      {
        foreach (var factor in m_Factors.Values)
        {
          float f = factor.Evaluate();
          Debug.Log($"<{nameof(DeviceDecider)}>  {factor} evaluated to weight {f}");
          sum += f;
        }

        Debug.Log($"<{nameof(DeviceDecider)}>  FINAL SUM = {sum}; (decision = {sum >= m_Threshold})");
      }
      else
      {
        foreach (var factor in m_Factors.Values)
        {
          sum += factor.Evaluate();
        }
      }

      return sum >= m_Threshold;
    }

    public bool DecideShort()
    {
      // this version "earlies out" by short-circuiting on exceeding the threshold.

      if (IsDisabled(out bool decision))
      {
        if (m_Verbose)
        {
          Debug.Log($"<{nameof(DeviceDecider)}>  is disabled. Default decision: {decision}");
        }

        return decision;
      }

      float sum = 0f;

      if (m_Verbose)
      {
        foreach (var factor in m_Factors.Values)
        {
          float f = factor.Evaluate();
          Debug.Log($"<{nameof(DeviceDecider)}>  {factor} evaluated to weight {f}");
          sum += f;

          if (sum >= m_Threshold)
            break;
        }

        Debug.Log($"<{nameof(DeviceDecider)}>  FINAL SUM = {sum}; (decision = {sum >= m_Threshold})");
        return sum >= m_Threshold;
      }

      foreach (var factor in m_Factors.Values)
      {
        sum += factor.Evaluate();
        if (sum >= m_Threshold)
          return true;
      }

      return false;
    }


    public IEnumerable<DeviceEvaluator> GetContinuousFactors()
    {
      foreach (var (key,val) in m_Factors)
      {
        if (val.IsContinuous)
          yield return val;
      }
    }

    public IEnumerable<DeviceEvaluator> GetDiscreteFactors()
    {
      foreach (var (key,val) in m_Factors)
      {
        if (val.IsDiscrete)
          yield return val;
      }
    }

  } // end class DeviceDecider

}
