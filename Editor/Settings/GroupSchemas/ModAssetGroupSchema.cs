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

		protected virtual string BuildPath => GetBuildPath(null);
		protected virtual string LoadPath => GetLoadPath(null);

		public override string GetBuildPath(AddressableAssetSettings aaSettings) => throw new NotImplementedException();

		public override void SetBuildPath(AddressableAssetSettings aaSettings, string name) => throw new NotImplementedException();

		public override string GetLoadPath(AddressableAssetSettings aaSettings) => throw new NotImplementedException();

		public override void SetLoadPath(AddressableAssetSettings aaSettings, string name) => throw new NotImplementedException();

		protected override void ShowPaths(SerializedObject so) {

			EditorGUILayout.LabelField("Build Path:", BuildPath);
			EditorGUILayout.LabelField("Load Path:", LoadPath);

		}

		protected override void ShowPathsMulti(SerializedObject so, List<AddressableAssetGroupSchema> otherBundledSchemas, ref List<Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase>> queuedChanges) { }

		protected override void ShowSelectedPropertyPathPair(SerializedObject so) { }

		internal override int DetermineSelectedIndex(List<ProfileGroupType> groupTypes, int defaultValue, AddressableAssetSettings addressableAssetSettings, HashSet<string> vars) => defaultValue;

		protected override void ShowPathsPreview(bool showMixedValue) {

			EditorGUI.indentLevel++;

			m_ShowPaths = EditorGUILayout.Foldout(m_ShowPaths, m_PathsPreviewGUIContent, true);

			if(m_ShowPaths) {

				EditorStyles.helpBox.fontSize = 12;

				EditorGUILayout.HelpBox($"Build Path: {BuildPath}", MessageType.None);
				EditorGUILayout.HelpBox($"Load Path: {LoadPath}", MessageType.None);

			}

			EditorGUI.indentLevel--;

		}

	}

}