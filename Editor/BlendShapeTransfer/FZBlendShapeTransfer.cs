using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
using EUI = FZTools.EditorUtils.UI;
using ELayout = FZTools.EditorUtils.Layout;
using static FZTools.FZToolsConstants;

namespace FZTools
{
    public class FZBlendShapeTransfer : EditorWindow
    {
        [SerializeField] GameObject avatar;
        [SerializeField] AnimationClip anim;
        VRCAvatarDescriptor AvatarDescriptor => avatar != null ? avatar.GetComponent<VRCAvatarDescriptor>() : null;
        List<SkinnedMeshRenderer> SkinnedMeshRenderers => AvatarDescriptor != null ? AvatarDescriptor.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList() : new List<SkinnedMeshRenderer>();
        List<string> RenderersObjPath => SkinnedMeshRenderers.Select(e => e.gameObject.GetGameObjectPath(true)).ToList();
        String[] MeshNames => SkinnedMeshRenderers.Select(smr => smr.gameObject.name).ToArray();
        private SkinnedMeshRenderer selected;
        private String selectedMeshPath;
        private int selectedIndex = -1;
        private int prevSelectedIndex = -1;
        private bool checkValid = true;
        Vector2 scrollPos;


        [MenuItem("FZTools/BlendShapeTransfer")]
        private static void OpenWindow()
        {
            var window = GetWindow<FZBlendShapeTransfer>();
            window.titleContent = new GUIContent("BlendShapeTransfer");
        }

        private void OnGUI()
        {
            ELayout.Horizontal(() =>
            {
                EUI.Space();
                ELayout.Vertical(() =>
                {
                    EUI.Space(2);
                    EUI.Label("Target Avatar");
                    EUI.ChangeCheck(
                        () => EUI.ObjectField<GameObject>(ref avatar),
                        () =>
                        {
                            selectedIndex = 0;
                            CheckBlendShape();
                        });
                    EUI.Space();
                    var text = "以下の機能を提供します\n"
                            + "・AnimationClipの値をメッシュのBlendshapeに転写\n"
                            + "・メッシュのBlendshapeの値をAnimationClipに転写";
                    EUI.InfoBox(text);

                    EUI.Space(2);
                    EUI.Label("Skinned Mesh Renderer");
                    EUI.Popup(ref selectedIndex, MeshNames, null);
                    if (prevSelectedIndex != selectedIndex)
                    {
                        selected = SkinnedMeshRenderers[selectedIndex];
                        selectedMeshPath = RenderersObjPath[selectedIndex];
                        prevSelectedIndex = selectedIndex;
                        CheckBlendShape();
                    }
                    EUI.Space();
                    EUI.Label("Animation Clip");
                    EUI.ChangeCheck(
                        () => EUI.ObjectField<AnimationClip>(ref anim),
                        () =>
                        {
                            CheckBlendShape();
                        });
                    EUI.Space(2);
                    if (!checkValid)
                    {
                        var warnText = "Meshに存在しないShapeがAnimationClipに含まれています\n"
                                    + "Mesh側に存在しないShapeについては無視して転写されます";
                        EUI.InfoBox(warnText);
                    }
                    EUI.Space(2);
                    EUI.Button("AnimationClip -> メッシュ", AnimToMesh);
                    EUI.Space();
                    EUI.Button("メッシュ -> AnimationClip", MeshToAnim);
                });
                EUI.Space();
            });

        }

        private void CheckBlendShape()
        {
            if (selected == null || anim == null)
            {
                return;
            }

            var curves = anim.GetBindingCurves();
            curves.ForEach(c =>
            {
                var pn = c.propertyName.Replace("blendShape.", "");
                if (selected.sharedMesh.GetBlendShapeIndex(pn) < 0)
                {
                    checkValid = false;
                    return;
                }

                checkValid = true;
            });
        }

        private void AnimToMesh()
        {
            var curves = anim.GetBindingCurves();
            curves.ForEach(c =>
            {
                string pn = c.propertyName.Replace("blendShape.", "");
                int bi = selected.sharedMesh.GetBlendShapeIndex(pn);
                selected.SetBlendShapeWeight(bi, AnimationUtility.GetEditorCurve(anim, c).keys[0].value);
            });

        }
        public void MeshToAnim()
        {
            var usesViseme = AvatarDescriptor.lipSync == VRCAvatarDescriptor.LipSyncStyle.VisemeBlendShape;
            var isBlendshapeEyelids = AvatarDescriptor.customEyeLookSettings.eyelidType == VRCAvatarDescriptor.EyelidType.Blendshapes;

            var visemeBlendShapes = usesViseme ? AvatarDescriptor?.VisemeBlendShapes?.ToList() : null;
            var eyelidsBlendShapes = isBlendshapeEyelids ? AvatarDescriptor.customEyeLookSettings.eyelidsBlendshapes.Select(index => selected.sharedMesh.GetBlendShapeName(index)).ToList() : null;

            for (int i = 0; i < selected.sharedMesh.blendShapeCount; i++)
            {
                string shapeName = selected.sharedMesh.GetBlendShapeName(i);
                if ((usesViseme && visemeBlendShapes.Contains(shapeName)) || (isBlendshapeEyelids && eyelidsBlendShapes.Contains(shapeName)))
                {
                    continue;
                }
                anim.AddAnimationCurve(new Keyframe(0,selected.GetBlendShapeWeight(i)), selectedMeshPath, FZToolsConstants.AnimClipParam.BlendShape(shapeName),typeof(SkinnedMeshRenderer));
            }
        }
    }
}
