using ProjectBlade.Core.Runtime.Modding;

using System;
using System.Collections.Generic;
using System.ComponentModel;

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

		public override string GetBuildPath(AddressableAssetSettings aaSettings) => $"Build/{(mod == null ? "unassigned-assets" : mod.Id)}/Content";
		public override void SetBuildPath(AddressableAssetSettings aaSettings, string name) => throw new NotImplementedException();
		public override bool BuildPathExists() => true;

		public override string GetLoadPath(AddressableAssetSettings aaSettings) => mod == null ? null : $"Mods/{mod.Id}/Content";
		public override void SetLoadPath(AddressableAssetSettings aaSettings, string name) => throw new NotImplementedException();
		public override bool LoadPathExists() => true;

		public override void OnGUI() {

			EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty("mod"));

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

		internal override int DetermineSelectedIndex(List<ProfileGroupType> groupTypes,
														int defaultValue,
														AddressableAssetSettings addressableAssetSettings,
														HashSet<string> vars) => defaultValue;

	}

}