using ProjectBlade.Core.Runtime.Modding;

using System;
using System.ComponentModel;
using System.IO;

using UnityEditor.AddressableAssets.HostingServices;

using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas {
	/// <summary>
	/// Schema used for bundled mod asset groups.
	/// </summary>
	[DisplayName("Mod Content Packing & Loading")]
	public class ModAssetGroupSchema : BundledAssetGroupSchemaBase, IHostingServiceConfigurationProvider, ISerializationCallbackReceiver {

		public ModBase mod;

		protected virtual string BuildPath => GetBuildPath(null);
		protected virtual string LoadPath => GetLoadPath(null);

		public override string GetBuildPath(AddressableAssetSettings aaSettings) => $"Build/Mods/{(mod == null ? "unassigned-assets" : mod.Id)}/Content";
		public override void SetBuildPath(AddressableAssetSettings aaSettings, string name) => throw new NotImplementedException();
		public override bool BuildPathExists() => true;

		public override string GetLoadPath(AddressableAssetSettings aaSettings) => mod == null ? null : $"Mods/{mod.Id}/Content";
		public override void SetLoadPath(AddressableAssetSettings aaSettings, string name) => throw new NotImplementedException();
		public override bool LoadPathExists() => true;

		public override void OnGUI() {

			EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty("mod"));

			SchemaSerializedObject.ApplyModifiedProperties();

			string groupPath = AssetDatabase.GetAssetPath(Group);
			string desiredGroupPath = DesiredGroupLocation;

			UnityEngine.GUI.enabled = !IsGroupInDesiredLocation;

			if(GUILayout.Button("Move Group into Mod package")) {

				Directory.CreateDirectory(Path.GetDirectoryName(desiredGroupPath));

				AssetDatabase.MoveAsset(groupPath, desiredGroupPath);

			}

			string desiredPath = DesiredLocation;

			UnityEngine.GUI.enabled = !IsInDesiredLocation;

			if(GUILayout.Button("Move Schema into Mod package")) {

				Directory.CreateDirectory(Path.GetDirectoryName(desiredPath));

				AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(this), desiredPath);

			}

			UnityEngine.GUI.enabled = true;

			base.OnGUI();

		}

		protected override void ShowSelectedPropertyPathPair() {

			EditorGUI.indentLevel++;

			m_ShowPaths = EditorGUILayout.Foldout(m_ShowPaths, m_PathsPreviewGUIContent, true);

			if(m_ShowPaths) {

				EditorStyles.helpBox.fontSize = 12;

				EditorGUILayout.HelpBox($"Build Path: {BuildPath}", MessageType.None);
				EditorGUILayout.HelpBox($"Load Path: {LoadPath}", MessageType.None);

			}

			EditorGUI.indentLevel--;

		}

		public override string DesiredLocation {

			get {

				if(mod == null) {

					return base.DesiredLocation;

				}

				string packagePath = PackageManager.PackageInfo.FindForAssetPath(AssetDatabase.GetAssetPath(mod)).assetPath;

				return Path.Combine(packagePath, "Assets", "Addressables", "Schemas", DesiredFileName);

			}

		}

		public virtual string DesiredGroupLocation {

			get {

				string packagePath = PackageManager.PackageInfo.FindForAssetPath(AssetDatabase.GetAssetPath(mod)).assetPath;

				return Path.Combine(packagePath, "Assets", "Addressables", "Groups", Path.GetFileName(AssetDatabase.GetAssetPath(Group)));

			}

		}

		public virtual bool IsGroupInDesiredLocation => Path.GetFullPath(AssetDatabase.GetAssetPath(Group)).Equals(Path.GetFullPath(DesiredGroupLocation));

	}

}