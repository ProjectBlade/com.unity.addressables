using ProjectBlade.Core.Runtime.Modding;

using System;
using System.IO;
using System.Linq;

using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.Util;



#if UNITY_2022_2_OR_NEWER
#endif

namespace UnityEditor.AddressableAssets.Build.DataBuilders {
	/// <summary>
	/// Build scripts used for player w/ mods builds and running with bundles in the editor.
	/// </summary>
	[CreateAssetMenu(fileName = "BuildScriptPackedWithModCatalogs.asset", menuName = "Addressables/Content Builders/Build Script Packed With Mod Catalogs")]
	public class BuildScriptPackedWithModCatalogsMode : BuildScriptPackedMode {
		/// <inheritdoc />
		public override string Name => "Build Script Packed With Mod Catalogs";

		/// <inheritdoc />
		protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) {

			TResult result = base.DoBuild<TResult>(builderInput, aaContext);

			using(m_Log.ScopedStep(LogLevel.Info, "Generate JSON Catalogs for Mods")) {

				foreach(string modAssetPath in AssetDatabase.FindAssets($"t:{typeof(ModBase).Name}").Select(guid => AssetDatabase.GUIDToAssetPath(guid))) {

					string[] modAssetPathPartsPackageAndId = modAssetPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[..2];

					ModBase mod = AssetDatabase.LoadAssetAtPath<ModBase>(modAssetPath);

					ContentCatalogData contentCatalog = new($"Mod~{mod.Id}~ContentCatalog");

					AddressablesPlayerBuildResult addrResult = result as AddressablesPlayerBuildResult;

					if(result != null) {

						contentCatalog.m_BuildResultHash = HashingMethods.Calculate(addrResult.AssetBundleBuildResults
																			.Where(result =>
																					modAssetPathPartsPackageAndId.SequenceEqual(AssetDatabase.GetAssetPath(result.SourceAssetGroup)
																																					.Split(Path.DirectorySeparatorChar,
																																							Path.AltDirectorySeparatorChar)
																																					[..2]))
																			.Select(result => result.Hash)
																			.ToArray()).ToString();

					}

					contentCatalog.SetData(aaContext.locations
													.Where(entry =>
															modAssetPathPartsPackageAndId.SequenceEqual(entry.InternalId
																													.Split(Path.DirectorySeparatorChar,
																															Path.AltDirectorySeparatorChar)
																													[..2]) ||
															entry.InternalId.StartsWith(Path.Combine("Mods", mod.Id, "Content")))
													.OrderBy(entry => entry.InternalId)
													.ToList());

					contentCatalog.ResourceProviderData.AddRange(m_ResourceProviderData);

					foreach(Type type in aaContext.providerTypes) {

						contentCatalog.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(type));

					}

					contentCatalog.InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData(instanceProviderType.Value);
					contentCatalog.SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData(sceneProviderType.Value);

					string jsonText = null;

					using(m_Log.ScopedStep(LogLevel.Info, "Generating Json")) {

						jsonText = JsonUtility.ToJson(contentCatalog);

					}

					using(m_Log.ScopedStep(LogLevel.Info, "Hashing Catalog")) {

						contentCatalog.localHash = HashingMethods.Calculate(jsonText).ToString();

					}

					CreateModCatalogFiles(jsonText, builderInput, aaContext, mod, contentCatalog.localHash);

				}

			}

			return result;

		}

		internal bool CreateModCatalogFiles(string jsonText,
											AddressablesDataBuilderInput builderInput,
											AddressableAssetsBuildContext aaContext,
											ModBase mod,
											string catalogHash = null) {

			if(string.IsNullOrEmpty(jsonText) || builderInput == null || aaContext == null) {

				Addressables.LogError("Unable to create mod content catalog (Null arguments).");

				return false;

			}

			if(string.IsNullOrEmpty(catalogHash)) {

				catalogHash = HashingMethods.Calculate(jsonText).ToString();

			}

			string fileName = "catalog";
			string buildFolder = Path.Combine("Build", "Mods", mod.Id, "Content");

			string jsonBuildPath = Path.Combine(buildFolder, fileName + ".json");
			string hashBuildPath = Path.Combine(buildFolder, fileName + ".hash");

			WriteFile(jsonBuildPath, jsonText, builderInput.Registry);
			WriteFile(hashBuildPath, catalogHash, builderInput.Registry);

			return true;

		}

		/*static string[] CreateModCatalog(string jsonText,
											List<ResourceLocationData> locations,
											AddressableAssetSettings aaSettings,
											ModBase mod,
											AddressablesDataBuilderInput builderInput,
											ProviderLoadRequestOptions catalogLoadOptions,
											string contentHash) {


			string[] dependencyHashes = null;

			if(string.IsNullOrEmpty(contentHash)) {

				contentHash = HashingMethods.Calculate(jsonText).ToString();

			}

			string fileName = "catalog";//aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, "/catalog_" + builderInput.PlayerVersion);
			string buildFolder = Path.Combine("Build", "Mods", mod.Id, "Content");
			string loadFolder = Path.Combine("Mods", mod.Id, "Content");

			string jsonBuildPath = Path.Combine(buildFolder, fileName + ".json");
			string hashBuildPath = Path.Combine(buildFolder, fileName + ".hash");

			WriteFile(jsonBuildPath, jsonText, builderInput.Registry);
			WriteFile(hashBuildPath, contentHash, builderInput.Registry);

			dependencyHashes = new string[(int) ContentCatalogProvider.DependencyHashIndex.Count];
			dependencyHashes[(int) ContentCatalogProvider.DependencyHashIndex.Remote] = ResourceManagerRuntimeData.kCatalogAddress + "RemoteHash";
			dependencyHashes[(int) ContentCatalogProvider.DependencyHashIndex.Cache] = ResourceManagerRuntimeData.kCatalogAddress + "CacheHash";

			string hashLoadPath = Path.Combine(loadFolder, fileName + ".hash");

			ResourceLocationData hashLoadLocation = new(
				new[] {

					dependencyHashes[(int) ContentCatalogProvider.DependencyHashIndex.Remote]

				},
				hashLoadPath,
				typeof(TextDataProvider),
				typeof(string)
			) {

				Data = catalogLoadOptions.Copy()

			};

			locations.Add(hashLoadLocation);

#if UNITY_SWITCH

			var cacheLoadPath = hashLoadPath; // ResourceLocationBase does not allow empty string id
			
#else

			string cacheLoadPath = "{UnityEngine.Application.persistentDataPath}/com.unity.addressables/" + fileName + ".hash";

#endif

			ResourceLocationData cacheLoadLocation = new(
				new[] {

					dependencyHashes[(int) ContentCatalogProvider.DependencyHashIndex.Cache]

				},
				cacheLoadPath,
				typeof(TextDataProvider),
				typeof(string)
			) {

				Data = catalogLoadOptions.Copy()

			};

			locations.Add(cacheLoadLocation);

			return dependencyHashes;

		}*/

	}

}