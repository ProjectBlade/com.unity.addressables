using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using UnityEditor.AddressableAssets.HostingServices;

using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas {
	/// <summary>
	/// Schema used for bundled loader asset groups.
	/// </summary>
	[DisplayName("Loader Content Packing & Loading")]
	public class LoaderAssetGroupSchema : BundledAssetGroupSchemaBase, IHostingServiceConfigurationProvider, ISerializationCallbackReceiver {
		[FormerlySerializedAs("m_buildPath")]
		[SerializeField]
		[Tooltip("The path to copy asset bundles to.")]
		protected ProfileValueReference m_BuildPath = new();

		[FormerlySerializedAs("m_loadPath")]
		[SerializeField]
		[Tooltip("The path to load bundles from.")]
		protected ProfileValueReference m_LoadPath = new();

		public override string GetBuildPath(AddressableAssetSettings aaSettings) => m_BuildPath?.GetValue(aaSettings);
		public override void SetBuildPath(AddressableAssetSettings aaSettings, string name) => m_BuildPath?.SetVariableByName(aaSettings, name);
		public override bool BuildPathExists() => m_BuildPath != null;

		public override string GetLoadPath(AddressableAssetSettings aaSettings) => m_LoadPath?.GetValue(aaSettings);
		public override void SetLoadPath(AddressableAssetSettings aaSettings, string name) => m_LoadPath?.SetVariableByName(aaSettings, name);
		public override bool LoadPathExists() => m_LoadPath != null;

		/// <summary>
		/// Used to determine if dropdown should be custom
		/// </summary>
		private bool m_UseCustomPaths = false;

		protected void SetPathVariable(AddressableAssetSettings addressableAssetSettings,
										ref ProfileValueReference path,
										string newPathName,
										string oldPathName,
										List<string> variableNames) {

			if(path != null && path.HasValue(addressableAssetSettings)) {

				return;

			}

			bool hasNewPath = variableNames.Contains(newPathName);
			bool hasOldPath = variableNames.Contains(oldPathName);

			if(hasNewPath && string.IsNullOrEmpty(path?.Id)) {

				path = new ProfileValueReference();
				path.SetVariableByName(addressableAssetSettings, newPathName);

				SetDirty(true);

			}

			else if(hasOldPath && string.IsNullOrEmpty(path?.Id)) {

				path = new ProfileValueReference();
				path.SetVariableByName(addressableAssetSettings, oldPathName);

				SetDirty(true);

			}

			else if(!hasOldPath && !hasNewPath) {

				Debug.LogWarning("Default path variable " + newPathName + " not found when initializing BundledAssetGroupSchema. Please manually set the path via the groups window.");

			}

		}

		internal override void Validate() {

			if(Group != null && Group.Settings != null) {

				List<string> variableNames = Group.Settings.profileSettings.GetVariableNames();

				SetPathVariable(Group.Settings, ref m_BuildPath, AddressableAssetSettings.kLocalBuildPath, "LocalBuildPath", variableNames);
				SetPathVariable(Group.Settings, ref m_LoadPath, AddressableAssetSettings.kLocalLoadPath, "LocalLoadPath", variableNames);

			}

			base.Validate();

		}

		/// <summary>
		/// Impementation of ISerializationCallbackReceiver, used to set callbacks for ProfileValueReference changes.
		/// </summary>
		public override void OnAfterDeserialize() {

			m_BuildPath.OnValueChanged += s => SetDirty(true);
			m_LoadPath.OnValueChanged += s => SetDirty(true);

			base.OnAfterDeserialize();

		}

		protected virtual void ShowPaths() {

			ShowSelectedPropertyPath(nameof(m_BuildPath), null, ref m_BuildPath);
			ShowSelectedPropertyPath(nameof(m_LoadPath), null, ref m_LoadPath);

		}

		protected virtual void ShowPathsMulti(List<AddressableAssetGroupSchema> otherBundledSchemas,
												ref List<Action<BundledAssetGroupSchemaBase, BundledAssetGroupSchemaBase>> queuedChanges) {

			ShowSelectedPropertyMulti(nameof(m_BuildPath), null, otherBundledSchemas, ref queuedChanges, (src, dst) => {

				if(src is not LoaderAssetGroupSchema || dst is not LoaderAssetGroupSchema) {

					return;

				}

				(dst as LoaderAssetGroupSchema).m_BuildPath.Id = (src as LoaderAssetGroupSchema).m_BuildPath.Id;

				dst.SetDirty(true);

			}, m_BuildPath.Id, ref m_BuildPath);

			ShowSelectedPropertyMulti(nameof(m_LoadPath), null, otherBundledSchemas, ref queuedChanges, (src, dst) => {

				(dst as LoaderAssetGroupSchema).m_LoadPath.Id = (src as LoaderAssetGroupSchema).m_LoadPath.Id;

				dst.SetDirty(true);

			}, m_LoadPath.Id, ref m_LoadPath);

		}

		protected override void ShowSelectedPropertyPathPair() {

			List<ProfileGroupType> groupTypes = ProfileGroupType.CreateGroupTypes(settings.profileSettings.GetProfile(settings.activeProfileId), settings);

			List<string> options = groupTypes.Select(group => group.GroupTypePrefix).ToList();
			//Set selected to custom
			options.Add(AddressableAssetProfileSettings.customEntryString);

			//Determine selection and whether to show custom

			int? selected = DetermineSelectedIndex(groupTypes, options.Count - 1, settings, settings.profileSettings.GetAllVariableIds());

			m_UseCustomPaths = !selected.HasValue || selected == options.Count - 1;

			//Dropdown selector
			EditorGUI.BeginChangeCheck();

			int newIndex = EditorGUILayout.Popup(m_BuildAndLoadPathsGUIContent, selected ?? options.Count - 1, options.ToArray());

			if(EditorGUI.EndChangeCheck() && newIndex != selected) {

				SetPathPairOption(options, groupTypes, newIndex);

				EditorUtility.SetDirty(this);

			}

			if(m_UseCustomPaths) {

				ShowPaths();

			}

			ShowPathsPreview(false);

			EditorGUI.showMixedValue = false;

		}

		internal override int DetermineSelectedIndex(List<ProfileGroupType> groupTypes,
														int defaultValue,
														AddressableAssetSettings addressableAssetSettings,
														HashSet<string> vars) {

			int selected = defaultValue;

			if(addressableAssetSettings == null) {

				return defaultValue;

			}

			if(vars.Contains(m_BuildPath.Id) && vars.Contains(m_LoadPath.Id) && !m_UseCustomPaths) {

				for(int i = 0; i < groupTypes.Count; i++) {

					ProfileGroupType.GroupTypeVariable buildPathVar = groupTypes[i].GetVariableBySuffix("BuildPath");
					ProfileGroupType.GroupTypeVariable loadPathVar = groupTypes[i].GetVariableBySuffix("LoadPath");

					if(m_BuildPath.GetName(addressableAssetSettings) == groupTypes[i].GetName(buildPathVar) && m_LoadPath.GetName(addressableAssetSettings) == groupTypes[i].GetName(loadPathVar)) {

						selected = i;

						break;

					}

				}

			}

			return selected;

		}

		protected virtual void SetPathPairOption(List<string> options, List<ProfileGroupType> groupTypes, int newIndex) {

			if(options[newIndex] != AddressableAssetProfileSettings.customEntryString) {

				Undo.RecordObject(SchemaSerializedObject.targetObject, SchemaSerializedObject.targetObject.name + "Path Pair");

				m_BuildPath.SetVariableByName(settings, groupTypes[newIndex].GroupTypePrefix + ProfileGroupType.k_PrefixSeparator + "BuildPath");
				m_LoadPath.SetVariableByName(settings, groupTypes[newIndex].GroupTypePrefix + ProfileGroupType.k_PrefixSeparator + "LoadPath");

				m_UseCustomPaths = false;

			}

			else {

				Undo.RecordObject(SchemaSerializedObject.targetObject, SchemaSerializedObject.targetObject.name + "Path Pair");

				m_UseCustomPaths = true;

			}

		}

		protected virtual void ShowPathsPreview(bool showMixedValue) {

			EditorGUI.indentLevel++;

			m_ShowPaths = EditorGUILayout.Foldout(m_ShowPaths, m_PathsPreviewGUIContent, true);

			if(m_ShowPaths) {

				EditorStyles.helpBox.fontSize = 12;

				string baseBuildPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, m_BuildPath.Id);
				string baseLoadPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, m_LoadPath.Id);

				EditorGUILayout.HelpBox($"Build Path: {(showMixedValue ? "-" : settings.profileSettings.EvaluateString(settings.activeProfileId, baseBuildPathValue))}", MessageType.None);
				EditorGUILayout.HelpBox($"Load Path: {(showMixedValue ? "-" : settings.profileSettings.EvaluateString(settings.activeProfileId, baseLoadPathValue))}", MessageType.None);

			}

			EditorGUI.indentLevel--;

		}

	}

}