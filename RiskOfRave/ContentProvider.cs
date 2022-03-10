// using System.Collections;
// using RoR2.ContentManagement;
// 
// namespace RuneFoxMods
// {
//   internal class ContentProvider : IContentPackProvider
//   {
//     internal ContentPack contentPack = new ContentPack();
//     public string identifier => RiskOfRave.Mod_GUID;
// 
//     public void Initialize()
//     {
//       ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
//     }
// 
//     private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
//     {
//       addContentPackProvider(this);
//     }
// 
//     public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
//     {
//       //this.contentPack.identifier = this.identifier;
//       contentPack.bodyPrefabs.Add(Prefabs.bodyPrefabs.ToArray());
// 
//       args.ReportProgress(1f);
//       yield break;
//     }
// 
//     public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
//     {
//       ContentPack.Copy(this.contentPack, args.output);
//       args.ReportProgress(1f);
//       yield break;
//     }
// 
//     public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
//     {
//       args.ReportProgress(1f);
//       yield break;
//     }
// 
// 
//   }
// }
