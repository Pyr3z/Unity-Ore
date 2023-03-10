/*! @file       Objects/SceneLord.cs
 *  @author     Levi Perez (levi\@leviperez.dev)
 *  @date       2022-08-21
**/

using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Ore
{
  [CreateAssetMenu(menuName = "Ore/" + nameof(SceneLord), fileName = nameof(SceneLord))]
  public class SceneLord : OAssetSingleton<SceneLord>
  {
    [SerializeField]
    protected UnloadSceneOptions m_DefaultUnloadSceneOptions = UnloadSceneOptions.None;

    private static int s_CurrentLoadAsync = -1;


    [PublicAPI]
    public static void AppQuit()
    {
      Application.Quit();
    }

    [PublicAPI]
    public static void AddScene(int index)
    {
      var load = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
      load.allowSceneActivation = true;
    }

    [PublicAPI]
    public static void AddActiveScene(int index)
    {
      if (s_CurrentLoadAsync == index)
        return;

      if (-1 < s_CurrentLoadAsync)
      {
        Orator.WarnOnce($"Already loading scene #{s_CurrentLoadAsync}, can't queue another one!", Current);
        return;
      }

      s_CurrentLoadAsync = index;
      var load = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
      load.allowSceneActivation = true;
      load.completed += _ =>
      {
        s_CurrentLoadAsync = -1;
        var scene = SceneManager.GetSceneByBuildIndex(index);
        if (scene.isLoaded)
        {
          SceneManager.SetActiveScene(scene);
        }
      };
    }

    [PublicAPI]
    public static void LoadScene(int index)
    {
      if (s_CurrentLoadAsync == index)
        return;

      if (-1 < s_CurrentLoadAsync)
      {
        Orator.WarnOnce($"Already loading scene #{s_CurrentLoadAsync}, can't queue another one!", Current);
        return;
      }

      s_CurrentLoadAsync = index;

      var load = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);

      load.allowSceneActivation = true;

      load.completed += _ =>
      {
        s_CurrentLoadAsync = -1;
        var curr = SceneManager.GetActiveScene();
        var next = SceneManager.GetSceneByBuildIndex(index);
        if (curr != next)
        {
          SceneManager.SetActiveScene(next);
          if (IsActive)
            SceneManager.UnloadSceneAsync(curr, Current.m_DefaultUnloadSceneOptions);
          else
            SceneManager.UnloadSceneAsync(curr, UnloadSceneOptions.None);
        }
      };
    }

  } // end class SceneLord
}
