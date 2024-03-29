/*! @file       Runtime/HashMap+Enumerator.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2022-10-04
**/

using System.Collections.Generic;
using System.Collections;

using JetBrains.Annotations;


namespace Ore
{
  public partial class HashMap<K,V>
  {
    public struct Enumerator :
      IEnumerator<(K key, V val)>,
      IEnumerator<KeyValuePair<K,V>>,
      IDictionaryEnumerator
    {
      public (K key, V val) Current      => (m_Bucket.Key,m_Bucket.Value);
      public K              CurrentKey   => m_Bucket.Key;
      public V              CurrentValue => m_Bucket.Value;

      public ref V CurrentValueRef => ref m_Parent.m_Buckets[m_Pos].Value;


      KeyValuePair<K,V> IEnumerator<KeyValuePair<K,V>>.Current => new KeyValuePair<K,V>(m_Bucket.Key, m_Bucket.Value);

      object IEnumerator.Current => (m_Bucket.Key, m_Bucket.Value);

      DictionaryEntry IDictionaryEnumerator.Entry => new DictionaryEntry(m_Bucket.Key, m_Bucket.Value);

      object IDictionaryEnumerator.Key => m_Bucket.Key;

      object IDictionaryEnumerator.Value => m_Bucket.Value;


      HashMap<K,V> m_Parent;

      internal Bucket m_Bucket;

      int m_Pos, m_Count, m_Version;
      int m_Unmapped;


      public Enumerator([NotNull] HashMap<K,V> forMap)
      {
        m_Parent   = forMap;
        m_Bucket   = default;
        m_Pos      = forMap.m_Buckets.Length;
        m_Count    = forMap.m_Count;
        m_Version  = forMap.m_Version;
        m_Unmapped = 0;
      }


      public bool MoveNext()
      {
        if (-- m_Count < 0 || -- m_Pos < 0)
        {
          return false;
        }

        CheckModified();

        do
        {
          m_Bucket = m_Parent.m_Buckets[m_Pos];
        }
        while (m_Bucket.MightBeEmpty() && m_Parent.m_KeyComparator.IsNone(m_Bucket.Key) && m_Pos --> 0);

        return m_Pos >= 0;
      }

      public void Reset()
      {
        if (m_Parent is null)
        {
          throw new System.InvalidOperationException("HashMap.Enumerator.Reset() cannot be called after disposal.");
        }

        ProcessChangedBuckets();

        m_Pos     = m_Parent.m_Buckets.Length;
        m_Count   = m_Parent.m_Count;
        m_Version = m_Parent.m_Version;
        m_Bucket  = default;
      }

      public void Dispose()
      {
        ProcessChangedBuckets();

        m_Parent = null;
        m_Bucket = default;
      }

      public void UnmapCurrent()
      {
        if (m_Count < 0 || m_Pos < 0 || m_Parent is null)
        {
          // it's fine just let it go
          return;
        }

        CheckModified();

        m_Parent.m_Buckets[m_Pos].Smear();

        m_Version = ++m_Parent.m_Version;

        ++ m_Unmapped;
      }

      public void RemapCurrent(V value)
      {
        if (m_Count < 0 || m_Pos < 0 || m_Parent is null)
        {
          throw new System.InvalidOperationException("HashMap.Enumerator.RemapCurrent(V) cannot be called after disposal.");
        }

        CheckModified();

        m_Parent.m_Buckets[m_Pos].Value = value;

        m_Version = ++m_Parent.m_Version;
      }


      void CheckModified()
      {
        if (m_Parent.m_Version != m_Version)
        {
          throw new System.InvalidOperationException("HashMap was modified elsewhere while iterating through it.");
        }
      }

      void ProcessChangedBuckets()
      {
        if (m_Parent is null)
          return;

        if (m_Unmapped == m_Parent.m_Count)
        {
          m_Parent.Clear();
        }
        else if (m_Unmapped > 0)
        {
          m_Parent.m_Count -= m_Unmapped;
        }

        m_Unmapped = 0;
      }

    } // end struct Enumerator
  } // end partial class HashMap
}