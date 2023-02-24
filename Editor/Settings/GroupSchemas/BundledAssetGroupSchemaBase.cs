using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.AddressableAssets.HostingServices;

using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas {
	/// <summary>
	/// Schema used for bundled asset groups.
	/// </summary>
	public abstract class BundledAssetGroupSchemaBase : AddressableAssetGroupSchema, IHostingServiceConfigurationProvider, ISerializationCallbackReceiver {
		/// <summary>
		/// Defines how bundles are created.
		/// </summary>
		public enum BundlePackingMode {
			/// <summary>
			/// Creates a bundle for all non-scene entries and another for all scenes entries.
			/// </summary>
			PackTogether,

			/// <summary>
			/// Creates a bundle per entry.  This is useful if each entry is a folder as all sub entries will go to the same bundle.
			/// </summary>
			PackSeparately,

			/// <summary>
			/// Creates a bundle per unique set of labels
			/// </summary>
			PackTogetherByLabel
		}

		/// <summary>
		/// Defines how internal bundles are named. This is used for both caching and for inter-bundle dependecies.  If possible, GroupGuidProjectIdHash should be used as it is stable and unique between projects.
		/// </summary>
		public enum BundleInternalIdMode {
			/// <summary>
			/// Use the guid of the group asset
			/// </summary>
			GroupGuid,

			/// <summary>
			/// Use the hash of the group asset guid and the project id
			/// </summary>
			GroupGuidProjectIdHash,

			/// <summary>
			/// Use the hash of the group asset, the project id and the guids of the entries in the group
			/// </summary>
			GroupGuidProjectIdEntriesHash
		}

		/// <summary>
		/// Options for compressing bundles in this group.
		/// </summary>
		public enum BundleCompressionMode {
			/// <summary>
			/// Use to indicate that bundles will not be compressed.
			/// </summary>
			Uncompressed,

			/// <summary>
			/// Use to indicate that bundles will be compressed using the LZ4 compression algorithm.
			/// </summary>
			LZ4,

			/// <summary>
			/// Use to indicate that bundles will be compressed using the LZMA compression algorithm.
			/// </summary>
			LZMA
		}

		[SerializeField]
		protected BundleInternalIdMode m_InternalBundleIdMode = BundleInternalIdMode.GroupGuidProjectIdHash;

		/// <summary>
		/// Internal bundle naming mode
		/// </summary>
		public virtual BundleInternalIdMode InternalBundleIdMode {
			get => m_InternalBundleIdMode;
			set {
				if(m_InternalBundleIdMode != value) {
					m_InternalBundleIdMode = value;
					SetDirty(true);
				}
			}
		}

		[SerializeField]
		protected BundleCompressionMode m_Compression = BundleCompressionMode.LZ4;

		/// <summary>
		/// Build compression.
		/// </summary>
		public virtual BundleCompressionMode Compression {
			get => m_Compression;
			set {
				if(m_Compression != value) {
					m_Compression = value;
					SetDirty(true);
				}
			}
		}

		/// <summary>
		/// Options for internal id of assets in bundles.
		/// </summary>
		public enum AssetNamingMode {
			/// <summary>
			/// Use to identify assets by their full path.
			/// </summary>
			FullPath,

			/// <summary>
			/// Use to identify assets by their filename only.  There is a risk of collisions when assets in different folders have the same filename.
			/// </summary>
			Filename,

			/// <summary>
			/// Use to identify assets by their asset guid.  This will save space over using the full path and will be stable if assets move in the project.
			/// </summary>
			GUID,

			/// <summary>
			/// This method attempts to use the smallest identifier for internal asset ids.  For asset bundles with very few items, this can save a significant amount of space in the content catalog.
			/// </summary>
			Dynamic
		}

		[SerializeField]
		protected bool m_IncludeAddressInCatalog = true;

		[SerializeField]
		protected bool m_IncludeGUIDInCatalog = true;

		[SerializeField]
		protected bool m_IncludeLabelsInCatalog = true;

		/// <summary>
		/// If enabled, addresses are included in the content catalog.  This is required if assets are to be loaded via their main address.
		/// </summary>
		public virtual bool IncludeAddressInCatalog {
			get => m_IncludeAddressInCatalog;
			set {
				if(m_IncludeAddressInCatalog != value) {
					m_IncludeAddressInCatalog = value;
					SetDirty(true);
				}
			}
		}

		/// <summary>
		/// If enabled, guids are included in content catalogs.  This is required if assets are to be loaded via AssetReference.
		/// </summary>
		public virtual bool IncludeGUIDInCatalog {
			get => m_IncludeGUIDInCatalog;
			set {
				if(m_IncludeGUIDInCatalog != value) {
					m_IncludeGUIDInCatalog = value;
					SetDirty(true);
				}
			}
		}

		/// <summary>
		/// If enabled, labels are included in the content catalogs.  This is required if labels are used at runtime load load assets.
		/// </summary>
		public virtual bool IncludeLabelsInCatalog {
			get => m_IncludeLabelsInCatalog;
			set {
				if(m_IncludeLabelsInCatalog != value) {
					m_IncludeLabelsInCatalog = value;
					SetDirty(true);
				}
			}
		}

		/// <summary>
		/// Internal Id mode for assets in bundles.
		/// </summary>
		public virtual AssetNamingMode InternalIdNamingMode {
			get => m_InternalIdNamingMode;
			set {
				m_InternalIdNamingMode = value;
				SetDirty(true);
			}
		}

		[SerializeField]
		[Tooltip("Indicates how the internal asset name will be generated.")]
		protected AssetNamingMode m_InternalIdNamingMode = AssetNamingMode.FullPath;


		/// <summary>
		/// Behavior for clearing old bundles from the cache.
		/// </summary>
		public enum CacheClearBehavior {
			/// <summary>
			/// Bundles are only removed from the cache when space is needed.
			/// </summary>
			ClearWhenSpaceIsNeededInCache,

			/// <summary>
			/// Bundles are removed from the cache when a newer version has been loaded successfully.
			/// </summary>
			ClearWhenWhenNewVersionLoaded,
		}

		[SerializeField]
		protected CacheClearBehavior m_CacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;

		/// <summary>
		/// Determines how other cached versions of asset bundles are cleared.
		/// </summary>
		public virtual CacheClearBehavior AssetBundledCacheClearBehavior {
			get => m_CacheClearBehavior;
			set {
				if(m_CacheClearBehavior != value) {
					m_CacheClearBehavior = value;
					SetDirty(true);
				}
			}
		}


		/// <summary>
		/// Gets the build compression settings for bundles in this group.
		/// </summary>
		/// <param name="bundleId">The bundle id.</param>
		/// <returns>The build compression.</returns>
		public virtual BuildCompression GetBuildCompressionForBundle(string bundleId) => m_Compression switch {

			BundleCompressionMode.Uncompressed => BuildCompression.Uncompressed,

			BundleCompressionMode.LZ4 => BuildCompression.LZ4,

			BundleCompressionMode.LZMA => BuildCompression.LZMA,

			_ => default,

		};

		[FormerlySerializedAs("m_includeInBuild")]
		[SerializeField]
		[Tooltip("If true, the assets in this group will be included in the build of bundles.")]
		protected bool m_IncludeInBuild = true;

		/// <summary>
		/// If true, the assets in this group will be included in the build of bundles.
		/// </summary>
		public virtual bool IncludeInBuild {
			get => m_IncludeInBuild;
			set {
				if(m_IncludeInBuild != value) {
					m_IncludeInBuild = value;
					SetDirty(true);
				}
			}
		}

		[SerializeField]
		[SerializedTypeRestriction(type = typeof(IResourceProvider))]
		[Tooltip("The provider type to use for loading assets from bundles.")]
		protected SerializedType m_BundledAssetProviderType;

		/// <summary>
		/// The provider type to use for loading assets from bundles.
		/// </summary>
		public virtual SerializedType BundledAssetProviderType {
			get => m_BundledAssetProviderType;
			internal set {
				m_BundledAssetProviderType = value;
				SetDirty(true);
			}
		}

		[SerializeField]
		[Tooltip("If true, the bundle and asset provider for assets in this group will get unique provider ids and will only provide for assets in this group.")]
		protected bool m_ForceUniqueProvider = false;

		/// <summary>
		/// If true, the bundle and asset provider for assets in this group will get unique provider ids and will only provide for assets in this group.
		/// </summary>
		public virtual bool ForceUniqueProvider {
			get => m_ForceUniqueProvider;
			set {
				if(m_ForceUniqueProvider != value) {
					m_ForceUniqueProvider = value;
					SetDirty(true);
				}
			}
		}

		[FormerlySerializedAs("m_useAssetBundleCache")]
		[SerializeField]
		[Tooltip("If true, the Hash value of the asset bundle is used to determine if a bundle can be loaded from the local cache instead of downloaded. (Only applies to remote asset bundles)")]
		protected bool m_UseAssetBundleCache = true;

		/// <summary>
		/// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
		/// </summary>
		public virtual bool UseAssetBundleCache {
			get => m_UseAssetBundleCache;
			set {
				if(m_UseAssetBundleCache != value) {
					m_UseAssetBundleCache = value;
					SetDirty(true);
				}
			}
		}

		[SerializeField]
		[Tooltip("If true, the CRC (Cyclic Redundancy Check) of the asset bundle is used to check the integrity.  This can be used for both local and remote bundles.")]
		protected bool m_UseAssetBundleCrc = true;

		/// <summary>
		/// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
		/// </summary>
		public virtual bool UseAssetBundleCrc {
			get => m_UseAssetBundleCrc;
			set {
				if(m_UseAssetBundleCrc != value) {
					m_UseAssetBundleCrc = value;
					SetDirty(true);
				}
			}
		}

		[SerializeField]
		[Tooltip("If true, the CRC (Cyclic Redundancy Check) of the asset bundle is used to check the integrity.")]
		protected bool m_UseAssetBundleCrcForCachedBundles = true;

		/// <summary>
		/// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
		/// </summary>
		public virtual bool UseAssetBundleCrcForCachedBundles {
			get => m_UseAssetBundleCrcForCachedBundles;
			set {
				if(m_UseAssetBundleCrcForCachedBundles != value) {
					m_UseAssetBundleCrcForCachedBundles = value;
					SetDirty(true);
				}
			}
		}

		[SerializeField]
		[Tooltip("If true, local asset bundles will be loaded through UnityWebRequest.")]
		protected bool m_UseUWRForLocalBundles = false;

		/// <summary>
		/// If true, local asset bundles will be loaded through UnityWebRequest.
		/// </summary>
		public virtual bool UseUnityWebRequestForLocalBundles {
			get => m_UseUWRForLocalBundles;
			set {
				if(m_UseUWRForLocalBundles != value) {
					m_UseUWRForLocalBundles = value;
					SetDirty(true);
				}
			}
		}

		[FormerlySerializedAs("m_timeout")]
		[SerializeField]
		[Tooltip("Attempt to abort after the number of seconds in timeout have passed, where the UnityWebRequest has received no data. (Only applies to remote asset bundles)")]
		protected int m_Timeout;

		/// <summary>
		/// Attempt to abort after the number of seconds in timeout have passed, where the UnityWebRequest has received no data.
		/// </summary>
		public virtual int Timeout {
			get => m_Timeout;
			set {
				if(m_Timeout != value) {
					m_Timeout = value;
					SetDirty(true);
				}
			}
		}

		[FormerlySerializedAs("m_chunkedTransfer")]
		[SerializeField]
		[Tooltip("Deprecated in 2019.3+. Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method. (Only applies to remote asset bundles)")]
		protected bool m_ChunkedTransfer;

		/// <summary>
		/// Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
		/// </summary>
		public virtual bool ChunkedTransfer {
			get => m_ChunkedTransfer;
			set {
				if(m_ChunkedTransfer != value) {
					m_ChunkedTransfer = value;
					SetDirty(true);
				}
			}
		}

		[FormerlySerializedAs("m_redirectLimit")]
		[SerializeField]
		[Tooltip("Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error. (Only applies to remote asset bundles)")]
		protected int m_RedirectLimit = -1;

		/// <summary>
		/// Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error.
		/// </summary>
		public virtual int RedirectLimit {
			get => m_RedirectLimit;
			set {
				if(m_RedirectLimit != value) {
					m_RedirectLimit = value;
					SetDirty(true);
				}
			}
		}

		[FormerlySerializedAs("m_retryCount")]
		[SerializeField]
		[Tooltip("Indicates the number of times the request will be retried.")]
		protected int m_RetryCount;

		/// <summary>
		/// Indicates the number of times the request will be retried.
		/// </summary>
		public virtual int RetryCount {
			get => m_RetryCount;
			set {
				if(m_RetryCount != value) {
					m_RetryCount = value;
					SetDirty(true);
				}
			}
		}

		/// <summary>
		/// The path to copy asset bundles to.
		/// </summary>
		public abstract string GetBuildPath(AddressableAssetSettings aaSettings);
		public abstract void SetBuildPath(AddressableAssetSettings aaSettings, string name);
		public abstract bool BuildPathExists();

		/// <summary>
		/// The path to load bundles from.
		/// </summary>
		public abstract string GetLoadPath(AddressableAssetSettings aaSettings);
		public abstract void SetLoadPath(AddressableAssetSettings aaSettings, string name);
		public abstract bool LoadPathExists();

		//placeholder for UrlSuffix support...
		internal virtual string UrlSuffix => string.Empty;

		[FormerlySerializedAs("m_bundleMode")]
		[SerializeField]
		[Tooltip("Controls how bundles are packed.  If set to PackTogether, a single asset bundle will be created for the entire group, with the exception of scenes, which are packed in a second bundle.  If set to PackSeparately, an asset bundle will be created for each entry in the group; in the case that an entry is a folder, one bundle is created for the folder and all of its sub entries.")]
		protected BundlePackingMode m_BundleMode = BundlePackingMode.PackTogether;

		/// <summary>
		/// Controls how bundles are packed.  If set to PackTogether, a single asset bundle will be created for the entire group, with the exception of scenes, which are packed in a second bundle.  If set to PackSeparately, an asset bundle will be created for each entry in the group; in the case that an entry is a folder, one bundle is created for the folder and all of its sub entries.
		/// </summary>
		public virtual BundlePackingMode BundleMode {
			get => m_BundleMode;
			set {
				if(m_BundleMode != value) {
					m_BundleMode = value;
					SetDirty(true);
				}
			}
		}

		/// <inheritdoc/>
		public virtual string HostingServicesContentRoot => GetBuildPath(Group.Settings);

		[FormerlySerializedAs("m_assetBundleProviderType")]
		[SerializeField]
		[SerializedTypeRestriction(type = typeof(IResourceProvider))]
		[Tooltip("The provider type to use for loading asset bundles.")]
		protected SerializedType m_AssetBundleProviderType;

		/// <summary>
		/// The provider type to use for loading asset bundles.
		/// </summary>
		public virtual SerializedType AssetBundleProviderType {
			get => m_AssetBundleProviderType;
			internal set {
				m_AssetBundleProviderType = value;
				SetDirty(true);
			}
		}

		/// <summary>
		/// Internal settings
		/// </summary>
		internal virtual AddressableAssetSettings settings => AddressableAssetSettingsDefaultObject.Settings;

		protected GUIContent m_BuildAndLoadPathsGUIContent = new("Build & Load Paths", "Paths to build or load AssetBundles from");
		protected GUIContent m_PathsPreviewGUIContent = new("Path Preview", "Preview of what the current paths will be evaluated to");

		/// <summary>
		/// Set default values taken from the assigned group.
		/// </summary>
		/// <param name="group">The group this schema has been added to.</param>
		protected override void OnSetGroup(AddressableAssetGroup group) {
			//this can happen during the load of the addressables asset
		}

		internal override void Validate() {

			if(m_AssetBundleProviderType.Value == null) {

				m_AssetBundleProviderType.Value = typeof(AssetBundleProvider);

			}

			if(m_BundledAssetProviderType.Value == null) {

				m_BundledAssetProviderType.Value = typeof(BundledAssetProvider);

			}

		}

		internal virtual string GetAssetLoadPath(string assetPath, HashSet<string> otherLoadPaths, Func<string, string> pathToGUIDFunc) => InternalIdNamingMode switch {

			AssetNamingMode.FullPath => assetPath,
			AssetNamingMode.Filename => assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ?
																			System.IO.Path.GetFileNameWithoutExtension(assetPath) :
																			System.IO.Path.GetFileName(assetPath),
			AssetNamingMode.GUID => pathToGUIDFunc(assetPath),
			AssetNamingMode.Dynamic => ((Func<string>) (() => {

				string g = pathToGUIDFunc(assetPath);

				if(otherLoadPaths == null) {

					return g;

				}

				int len = 1;
				string p = g[..len];

				while(otherLoadPaths.Contains(p)) {

					p = g[..++len];

				}

				otherLoadPaths.Add(p);

				return p;

			}))(),
			_ => assetPath,

		};

		/// <summary>
		/// Impementation of ISerializationCallbackReceiver, does nothing.
		/// </summary>
		public virtual void OnBeforeSerialize() { }

		/// <summary>
		/// Impementation of ISerializationCallbackReceiver, used to set callbacks for ProfileValueReference changes.
		/// </summary>
		public virtual void OnAfterDeserialize() {

			if(m_AssetBundleProviderType.Value == null) {

				m_AssetBundleProviderType.Value = typeof(AssetBundleProvider);

			}

			if(m_BundledAssetProviderType.Value == null) {

				m_BundledAssetProviderType.Value = typeof(BundledAssetProvider);

			}

		}

		/// <summary>
		/// Returns the id of the asset provider needed to load from this group.
		/// </summary>
		/// <returns>The id of the cached provider needed for this group.</returns>
		public virtual string GetAssetCachedProviderId() => ForceUniqueProvider ? string.Format("{0}_{1}", BundledAssetProviderType.Value.FullName, Group.Guid) : BundledAssetProviderType.Value.FullName;

		/// <summary>
		/// Returns the id of the bundle provider needed to load from this group.
		/// </summary>
		/// <returns>The id of the cached provider needed for this group.</returns>
		public virtual string GetBundleCachedProviderId() => ForceUniqueProvider ? string.Format("{0}_{1}", AssetBundleProviderType.Value.FullName, Group.Guid) : AssetBundleProviderType.Value.FullName;

		/// <summary>
		/// Used to determine how the final bundle name should look.
		/// </summary>
		public enum BundleNamingStyle {
			/// <summary>
			/// Use to indicate that the hash should be appended to the bundle name.
			/// </summary>
			AppendHash,

			/// <summary>
			/// Use to indicate that the bundle name should not contain the hash.
			/// </summary>
			NoHash,

			/// <summary>
			/// Use to indicate that the bundle name should only contain the given hash.
			/// </summary>
			OnlyHash,

			/// <summary>
			/// Use to indicate that the bundle name should only contain the hash of the file name.
			/// </summary>
			FileNameHash
		}

		/// <summary>
		/// Used to draw the Bundle Naming popup
		/// </summary>
		[CustomPropertyDrawer(typeof(BundleNamingStyle))]
		protected class BundleNamingStylePropertyDrawer : PropertyDrawer {
			/// <summary>
			/// Custom Drawer for the BundleNamingStyle in order to display easier to understand display names.
			/// </summary>
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				DrawGUI(position, property, label);
			}

			internal static int DrawGUI(Rect position, SerializedProperty property, GUIContent label) {
				bool showMixedValue = EditorGUI.showMixedValue;
				EditorGUI.BeginProperty(position, label, property);
				EditorGUI.showMixedValue = showMixedValue;

				GUIContent[] contents = new GUIContent[4];
				contents[0] = new GUIContent("Filename", "Leave filename unchanged.");
				contents[1] = new GUIContent("Append Hash to Filename", "Append filename with the AssetBundle content hash.");
				contents[2] = new GUIContent("Use Hash of AssetBundle", "Replace filename with AssetBundle hash.");
				contents[3] = new GUIContent("Use Hash of Filename", "Replace filename with hash of filename.");

				int enumValue = property.enumValueIndex;
				enumValue = enumValue == 0 ? 1 : enumValue == 1 ? 0 : enumValue;

				EditorGUI.BeginChangeCheck();
				int newValue = EditorGUI.Popup(position, new GUIContent(label.text, "Controls how the output AssetBundle's will be named."), enumValue, contents);
				if(EditorGUI.EndChangeCheck()) {
					newValue = newValue == 0 ? 1 : newValue == 1 ? 0 : newValue;
					property.enumValueIndex = newValue;
				}

				EditorGUI.EndProperty();
				return newValue;
			}

		}

		[SerializeField]
		protected BundleNamingStyle m_BundleNaming;

		/// <summary>
		/// Naming style to use for generated AssetBundle(s).
		/// </summary>
		public BundleNamingStyle BundleNaming {
			get => m_BundleNaming;
			set {
				if(m_BundleNaming != value) {
					m_BundleNaming = value;
					SetDirty(true);
				}
			}
		}

		[SerializeField]
		protected AssetLoadMode m_AssetLoadMode;

		/// <summary>
		/// Will load all Assets into memory from the AssetBundle after the AssetBundle is loaded.
		/// </summary>
		public AssetLoadMode AssetLoadMode {
			get => m_AssetLoadMode;
			set {
				if(m_AssetLoadMode != value) {
					m_AssetLoadMode = value;
					SetDirty(true);
				}
			}
		}

		protected bool m_ShowPaths = true;

		/// <summary>
		/// Used for drawing properties in the inspector.
		/// </summary>
		public override void ShowAllProperties() {

			m_ShowPaths = true;

			AdvancedOptionsFoldout.IsActive = true;

		}

		/// <inheritdoc/>
		public override void OnGUI() {

			ShowSelectedPropertyPathPair();

			AdvancedOptionsFoldout.IsActive = GUI.AddressablesGUIUtility.FoldoutWithHelp(AdvancedOptionsFoldout.IsActive, new GUIContent("Advanced Options"), () => {

				string url = AddressableAssetUtility.GenerateDocsURL("editor/groups/ContentPackingAndLoadingSchema.html#advanced-options");

				Application.OpenURL(url);

			});

			if(AdvancedOptionsFoldout.IsActive) {

				ShowAdvancedProperties();

			}

			SchemaSerializedObject.ApplyModifiedProperties();

		}

		/// <inheritdoc/>
		public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas) {

			List<Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase>> queuedChanges = null;

			List<BundledAssetGroupSchemaBase> otherBundledSchemas = otherSchemas.Where(schema => schema.GetType() == GetType()).Cast<BundledAssetGroupSchemaBase>().ToList();

			foreach(BundledAssetGroupSchemaBase schema in otherBundledSchemas) {

				schema.m_ShowPaths = m_ShowPaths;

			}

			EditorGUI.BeginChangeCheck();

			AdvancedOptionsFoldout.IsActive = GUI.AddressablesGUIUtility.BeginFoldoutHeaderGroupWithHelp(AdvancedOptionsFoldout.IsActive, new GUIContent("Advanced Options"), () => {

				string url = AddressableAssetUtility.GenerateDocsURL("editor/groups/ContentPackingAndLoadingSchema.html#advanced-options");

				Application.OpenURL(url);

			}, 10);

			if(AdvancedOptionsFoldout.IsActive) {

				ShowAdvancedPropertiesMulti(otherSchemas, ref queuedChanges);

			}

			EditorGUI.EndFoldoutHeaderGroup();

			SchemaSerializedObject.ApplyModifiedProperties();

			if(queuedChanges != null) {

				Undo.SetCurrentGroupName("bundledAssetGroupSchemasUndos");

				foreach(BundledAssetGroupSchemaBase schema in otherBundledSchemas) {

					Undo.RecordObject(schema, "BundledAssetGroupSchema" + schema.name);

				}

				foreach(Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase> change in queuedChanges) {

					foreach(BundledAssetGroupSchemaBase schema in otherBundledSchemas) {

						change.Invoke(this, schema);

					}

				}

			}

			Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

		}

		internal static GUI.FoldoutSessionStateValue AdvancedOptionsFoldout = new("Addressables.BundledAssetGroup.AdvancedOptions");

		protected GUIContent m_CompressionContent = new("Asset Bundle Compression", "Compression method to use for asset bundles.");
		protected GUIContent m_IncludeInBuildContent = new("Include in Build", "If disabled, these bundles will not be included in the build.");
		protected GUIContent m_ForceUniqueProviderContent = new("Force Unique Provider", "If enabled, this option forces bundles loaded from this group to use a unique provider.");
		protected GUIContent m_UseAssetBundleCacheContent = new("Use Asset Bundle Cache", "If enabled and supported, the device will cache  asset bundles.");
		protected GUIContent m_AssetBundleCrcContent = new("Asset Bundle CRC", "Defines which Asset Bundles will have their CRC checked when loading to ensure correct content.");

		protected GUIContent[] m_CrcPopupContent = new GUIContent[] {

			new("Disabled", "Bundles will not have their CRC checked when loading."),
			new("Enabled, Including Cached", "All Bundles will have their CRC checked when loading."),
			new("Enabled, Excluding Cached", "Bundles that have already been downloaded and cached will not have their CRC check when loading, otherwise CRC check will be performed.")

		};

		protected GUIContent m_UseUWRForLocalBundlesContent = new("Use UnityWebRequest for Local Asset Bundles", "If enabled, local asset bundles will load through UnityWebRequest.");
		protected GUIContent m_TimeoutContent = new("Request Timeout", "The timeout with no download activity (in seconds) for the Http request.");
		protected GUIContent m_ChunkedTransferContent = new("Use Http Chunked Transfer", "If enabled, the Http request will use chunked transfers.");
		protected GUIContent m_RedirectLimitContent = new("Http Redirect Limit", "The redirect limit for the Http request.");

		protected GUIContent m_RetryCountContent = new("Retry Count",
			"The number of times to retry the http request. Note that a retry count of 0 allows auto-downloading asset bundles that fail to load from the cache. Set to -1 to prevent this auto-downloading behavior.");

		protected GUIContent m_IncludeAddressInCatalogContent = new("Include Addresses in Catalog",
			"If disabled, addresses from this group will not be included in the catalog.  This is useful for reducing the size of the catalog if addresses are not needed.");

		protected GUIContent m_IncludeGUIDInCatalogContent = new("Include GUIDs in Catalog",
			"If disabled, guids from this group will not be included in the catalog.  This is useful for reducing the size of the catalog if guids are not needed.");

		protected GUIContent m_IncludeLabelsInCatalogContent = new("Include Labels in Catalog",
			"If disabled, labels from this group will not be included in the catalog.  This is useful for reducing the size of the catalog if labels are not needed.");

		protected GUIContent m_InternalIdNamingModeContent = new("Internal Asset Naming Mode",
			"Mode for naming assets internally in bundles.  This can reduce the size of the catalog by replacing long paths with shorter strings.");

		protected GUIContent m_InternalBundleIdModeContent = new("Internal Bundle Id Mode",
			$"Specifies how the internal id of the bundle is generated.  This must be set to {BundleInternalIdMode.GroupGuid} or {BundleInternalIdMode.GroupGuidProjectIdHash} to ensure proper caching on device.");

		protected GUIContent m_CacheClearBehaviorContent = new("Cache Clear Behavior", "Controls how old cached asset bundles are cleared.");
		protected GUIContent m_BundleModeContent = new("Bundle Mode", "Controls how bundles are created from this group.");
		protected GUIContent m_BundleNamingContent = new("Bundle Naming Mode", "Controls the final file naming mode for bundles in this group.");

		protected GUIContent m_AssetLoadModeContent = new("Asset Load Mode", "Determines how Assets are loaded when accessed." +
																			  "\n- Requested Asset And Dependencies, will only load the requested Asset (Recommended)." +
																			  "\n- All Packed Assets And Dependencies, will load all Assets that are packed together. Best used when loading all Assets into memory is required.");

		protected GUIContent m_AssetProviderContent = new("Asset Provider", "The provider to use for loading assets out of AssetBundles");
		protected GUIContent m_BundleProviderContent = new("Asset Bundle Provider", "The provider to use for loading AssetBundles (not the assets within bundles)");

		protected virtual void ShowAdvancedProperties() {

			SerializedObject so = SchemaSerializedObject;

			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_Compression)), m_CompressionContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeInBuild)), m_IncludeInBuildContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_ForceUniqueProvider)), m_ForceUniqueProviderContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_UseAssetBundleCache)), m_UseAssetBundleCacheContent, true);

			CRCPropertyPopupField();

			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_UseUWRForLocalBundles)), m_UseUWRForLocalBundlesContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_Timeout)), m_TimeoutContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_ChunkedTransfer)), m_ChunkedTransferContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_RedirectLimit)), m_RedirectLimitContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_RetryCount)), m_RetryCountContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeAddressInCatalog)), m_IncludeAddressInCatalogContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeGUIDInCatalog)), m_IncludeGUIDInCatalogContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeLabelsInCatalog)), m_IncludeLabelsInCatalogContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_InternalIdNamingMode)), m_InternalIdNamingModeContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_InternalBundleIdMode)), m_InternalBundleIdModeContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_CacheClearBehavior)), m_CacheClearBehaviorContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_BundleMode)), m_BundleModeContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_BundleNaming)), m_BundleNamingContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_AssetLoadMode)), m_AssetLoadModeContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_BundledAssetProviderType)), m_AssetProviderContent, true);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(m_AssetBundleProviderType)), m_BundleProviderContent, true);

		}

		protected virtual void CRCPropertyPopupField() {

			int enumIndex = 0;

			if(m_UseAssetBundleCrc) {

				enumIndex = m_UseAssetBundleCrcForCachedBundles ? 1 : 2;

			}

			int newEnumIndex = EditorGUILayout.Popup(m_AssetBundleCrcContent, enumIndex, m_CrcPopupContent);

			if(enumIndex != newEnumIndex) {

				if(newEnumIndex != 0) {

					if(!m_UseAssetBundleCrc) {

						SchemaSerializedObject.FindProperty("m_UseAssetBundleCrc").boolValue = true;

					}

					if(newEnumIndex == 1 && !m_UseAssetBundleCrcForCachedBundles) {

						SchemaSerializedObject.FindProperty("m_UseAssetBundleCrcForCachedBundles").boolValue = true;

					}

					else if(newEnumIndex == 2 && m_UseAssetBundleCrcForCachedBundles) {

						SchemaSerializedObject.FindProperty("m_UseAssetBundleCrcForCachedBundles").boolValue = false;

					}

				}

				else {

					SchemaSerializedObject.FindProperty("m_UseAssetBundleCrc").boolValue = false;

				}

			}

		}

		protected virtual void ShowAdvancedPropertiesMulti(List<AddressableAssetGroupSchema> otherBundledSchemas,
															ref List<Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase>> queuedChanges) {

			ShowSelectedPropertyMulti(nameof(m_Compression),
										m_CompressionContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.Compression = src.Compression,
										ref m_Compression);
			ShowSelectedPropertyMulti(nameof(m_IncludeInBuild),
										m_IncludeInBuildContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.IncludeInBuild = src.IncludeInBuild,
										ref m_IncludeInBuild);
			ShowSelectedPropertyMulti(nameof(m_ForceUniqueProvider),
										m_ForceUniqueProviderContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.ForceUniqueProvider = src.ForceUniqueProvider,
										ref m_ForceUniqueProvider);
			ShowSelectedPropertyMulti(nameof(m_UseAssetBundleCache),
										m_UseAssetBundleCacheContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.UseAssetBundleCache = src.UseAssetBundleCache,
										ref m_UseAssetBundleCache);
			ShowCustomGuiSelectedPropertyMulti(new string[] { nameof(m_UseAssetBundleCrc), nameof(m_UseAssetBundleCrcForCachedBundles) },
												m_AssetBundleCrcContent,
												otherBundledSchemas,
												ref queuedChanges,
												schema => CRCPropertyPopupField(),
												(src, dst) => {

													dst.UseAssetBundleCrc = src.UseAssetBundleCrc;
													dst.UseAssetBundleCrcForCachedBundles = src.UseAssetBundleCrcForCachedBundles;

												});
			ShowSelectedPropertyMulti(nameof(m_UseUWRForLocalBundles),
										m_UseUWRForLocalBundlesContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.UseUnityWebRequestForLocalBundles = src.UseUnityWebRequestForLocalBundles,
										ref m_UseUWRForLocalBundles);
			ShowSelectedPropertyMulti(nameof(m_Timeout),
										m_TimeoutContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.Timeout = src.Timeout,
										ref m_Timeout);
			ShowSelectedPropertyMulti(nameof(m_ChunkedTransfer),
										m_ChunkedTransferContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.ChunkedTransfer = src.ChunkedTransfer,
										ref m_ChunkedTransfer);
			ShowSelectedPropertyMulti(nameof(m_RedirectLimit),
										m_RedirectLimitContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.RedirectLimit = src.RedirectLimit,
										ref m_RedirectLimit);
			ShowSelectedPropertyMulti(nameof(m_RetryCount),
										m_RetryCountContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.RetryCount = src.RetryCount,
										ref m_RetryCount);
			ShowSelectedPropertyMulti(nameof(m_IncludeAddressInCatalog),
										m_IncludeAddressInCatalogContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.IncludeAddressInCatalog = src.IncludeAddressInCatalog,
										ref m_IncludeAddressInCatalog);
			ShowSelectedPropertyMulti(nameof(m_IncludeGUIDInCatalog),
										m_IncludeGUIDInCatalogContent,
										otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.IncludeGUIDInCatalog = src.IncludeGUIDInCatalog,
										ref m_IncludeGUIDInCatalog);
			ShowSelectedPropertyMulti(nameof(m_IncludeLabelsInCatalog),
										m_IncludeLabelsInCatalogContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.IncludeLabelsInCatalog = src.IncludeLabelsInCatalog,
										ref m_IncludeLabelsInCatalog);
			ShowSelectedPropertyMulti(nameof(m_InternalIdNamingMode),
										m_InternalIdNamingModeContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.InternalIdNamingMode = src.InternalIdNamingMode,
										ref m_InternalIdNamingMode);
			ShowSelectedPropertyMulti(nameof(m_InternalBundleIdMode),
										m_InternalBundleIdModeContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.InternalBundleIdMode = src.InternalBundleIdMode,
										ref m_InternalBundleIdMode);
			ShowSelectedPropertyMulti(nameof(m_CacheClearBehavior),
										m_CacheClearBehaviorContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.AssetBundledCacheClearBehavior = src.AssetBundledCacheClearBehavior,
										ref m_CacheClearBehavior);
			ShowSelectedPropertyMulti(nameof(m_BundleMode),
										m_BundleModeContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.BundleMode = src.BundleMode,
										ref m_BundleMode);
			ShowSelectedPropertyMulti(nameof(m_BundleNaming),
										m_BundleNamingContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.BundleNaming = src.BundleNaming,
										ref m_BundleNaming);
			ShowSelectedPropertyMulti(nameof(m_AssetLoadMode),
										m_AssetLoadModeContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.AssetLoadMode = src.AssetLoadMode,
										ref m_AssetLoadMode);
			ShowSelectedPropertyMulti(nameof(m_BundledAssetProviderType),
										m_AssetProviderContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.BundledAssetProviderType = src.BundledAssetProviderType,
										ref m_BundledAssetProviderType);
			ShowSelectedPropertyMulti(nameof(m_AssetBundleProviderType),
										m_BundleProviderContent, otherBundledSchemas,
										ref queuedChanges,
										(src, dst) => dst.AssetBundleProviderType = src.AssetBundleProviderType,
										ref m_AssetBundleProviderType);

		}

		protected virtual void ShowSelectedPropertyMulti<T>(string propertyName,
															GUIContent label,
															List<AddressableAssetGroupSchema> otherSchemas,
															ref List<Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase>> queuedChanges,
															Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase> a,
															ref T propertyValue) {

			SerializedProperty serializedProperty = SchemaSerializedObject.FindProperty(propertyName);

			Type propertySystemType = typeof(T);

			label ??= new(serializedProperty.displayName);

			ShowMixedValue(serializedProperty, otherSchemas, propertySystemType, propertyName);

			T newValue = default;

			SerializedPropertyType serializedPropertyType = SerializedPropertyType.Generic;

			EditorGUI.BeginChangeCheck();

			if(propertySystemType == typeof(bool)) {

				newValue = (T) (object) EditorGUILayout.Toggle(label, (bool) (object) propertyValue);

				serializedPropertyType = SerializedPropertyType.Boolean;

			}

			else if(propertySystemType.IsEnum) {

				serializedPropertyType = SerializedPropertyType.Enum;

				if(propertySystemType == typeof(BundleNamingStyle)) {

					Rect rect = EditorGUILayout.GetControlRect();

					int enumValue = BundleNamingStylePropertyDrawer.DrawGUI(rect, serializedProperty, label);

					newValue = (T) (object) enumValue;

				}

				else {

					int enumValue = Convert.ToInt32(EditorGUILayout.EnumPopup(label, (Enum) (object) propertyValue));

					newValue = (T) (object) enumValue;

				}

			}

			else if(propertySystemType == typeof(int)) {

				newValue = (T) (object) EditorGUILayout.IntField(label, (int) (object) propertyValue);

				serializedPropertyType = SerializedPropertyType.Integer;

			}

			else {

				EditorGUILayout.PropertyField(serializedProperty, label, true);

				SchemaSerializedObject.ApplyModifiedProperties();

			}

			if(EditorGUI.EndChangeCheck()) {

				if(serializedPropertyType != SerializedPropertyType.Generic) {

					HashSet<SerializedProperty> properties = new() { serializedProperty };

					foreach(AddressableAssetGroupSchema otherSchema in otherSchemas) {

						properties.Add(otherSchema.SchemaSerializedObject.FindProperty(propertyName));

					}

					foreach(SerializedProperty propertyForValueDestination in properties) {

						SerializedObject destinationSerializedObject = propertyForValueDestination.serializedObject;

						switch(serializedPropertyType) {

							case SerializedPropertyType.Boolean: {

								propertyForValueDestination.boolValue = (bool) (object) newValue;

								break;

							}

							case SerializedPropertyType.Integer: {

								propertyForValueDestination.intValue = (int) (object) newValue;

								break;

							}

							case SerializedPropertyType.Enum: {

								propertyForValueDestination.enumValueIndex = (int) (object) newValue;

								break;

							}

						}

						destinationSerializedObject.ApplyModifiedProperties();

					}

				}

				else if(a != null) {

					queuedChanges ??= new();

					queuedChanges.Add(a);


				}

			}

			EditorGUI.showMixedValue = false;

		}

		protected virtual void ShowCustomGuiSelectedPropertyMulti(string[] propertyNames,
																	GUIContent label,
																	List<AddressableAssetGroupSchema> otherSchemas,
																	ref List<Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase>> queuedChanges,
																	Action<BundledAssetGroupSchemaBase> guiAction,
																	Action<BundledAssetGroupSchemaBase,
																	BundledAssetGroupSchemaBase> a) {
			if(label == null) {

				return;

			}

			SerializedProperty[] props = new SerializedProperty[propertyNames.Length];

			for(int i = 0; i < propertyNames.Length; ++i) {

				props[i] = SchemaSerializedObject.FindProperty(propertyNames[i]);

			}

			for(int i = 0; i < propertyNames.Length; ++i) {

				if(EditorGUI.showMixedValue) {

					break;

				}

				ShowMixedValue(props[i], otherSchemas, null, propertyNames[i]);

			}

			EditorGUI.BeginChangeCheck();

			guiAction.Invoke(this);

			if(EditorGUI.EndChangeCheck()) {

				queuedChanges ??= new();

				queuedChanges.Add(a);

				EditorUtility.SetDirty(this);

			}

			EditorGUI.showMixedValue = false;

		}

		protected virtual void ShowSelectedPropertyMulti(string propertyName,
															GUIContent label,
															List<AddressableAssetGroupSchema> otherSchemas,
															ref List<Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase>> queuedChanges,
															Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase> a,
															string previousValue,
															ref ProfileValueReference currentValue) {

			SerializedProperty prop = SchemaSerializedObject.FindProperty(propertyName);

			ShowMixedValue(prop, otherSchemas, typeof(ProfileValueReference), propertyName);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(prop, label, true);

			if(EditorGUI.EndChangeCheck()) {

				string newValue = currentValue.Id;
				currentValue.Id = previousValue;

				Undo.RecordObject(SchemaSerializedObject.targetObject, SchemaSerializedObject.targetObject.name + propertyName);

				currentValue.Id = newValue;
				queuedChanges ??= new();
				queuedChanges.Add(a);

			}

			EditorGUI.showMixedValue = false;

		}

		protected virtual void ShowSelectedPropertyPath(string propertyName,
														GUIContent label,
														ref ProfileValueReference currentValue) {

			SerializedProperty prop = SchemaSerializedObject.FindProperty(propertyName);

			string previousValue = currentValue.Id;

			EditorGUI.BeginChangeCheck();
			//Current implementation using ProfileValueReferenceDrawer
			EditorGUILayout.PropertyField(prop, label, true);

			if(EditorGUI.EndChangeCheck()) {

				string newValue = currentValue.Id;
				currentValue.Id = previousValue;

				Undo.RecordObject(SchemaSerializedObject.targetObject, SchemaSerializedObject.targetObject.name + propertyName);

				currentValue.Id = newValue;

				EditorUtility.SetDirty(this);

			}

			EditorGUI.showMixedValue = false;

		}

		protected abstract void ShowSelectedPropertyPathPair();

		internal virtual int DetermineSelectedIndex(List<ProfileGroupType> groupTypes,
														int defaultValue,
														AddressableAssetSettings addressableAssetSettings,
														HashSet<string> vars) => defaultValue;

	}

}