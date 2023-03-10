/*! @file       Attributes/RequiredAttribute.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2023-02-28
**/

using JetBrains.Annotations;


namespace Ore
{
  [PublicAPI]
  [System.AttributeUsage(System.AttributeTargets.Field)]
  [System.Diagnostics.Conditional("UNITY_EDITOR")]
  public class RequiredAttribute : UnityEngine.PropertyAttribute
  {

    public string ErrorMessage;

    public RequiredAttribute()
    {
    }

    public RequiredAttribute([CanBeNull] string message)
    {
      ErrorMessage = message;
    }

  } // end class RequiredAttribute
}