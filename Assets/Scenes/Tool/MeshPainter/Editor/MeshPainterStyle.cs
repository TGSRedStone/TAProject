using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshPainter))]
[CanEditMultipleObjects]
public class MeshPainterStyle : Editor
{
    public override void OnInspectorGUI()
    {
        if (Check())
        {

        }
    }

    private bool Check()
    {
        bool passCheck = false;
        Transform select = Selection.activeTransform;
        if (select.GetComponent<MeshRenderer>().sharedMaterial.shader == Shader.Find("Fur/BaseFur"))
        {
            Texture furDataTex = select.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_DataTex");
            if (furDataTex == null)
            {
                EditorGUILayout.HelpBox("��ǰ���ʲ�����DataTex", MessageType.Error);
                if (GUILayout.Button("����DataTex"))
                {
                    CreateFurDataTex();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("��ǰ���Ƶ���ë����DataTex", MessageType.Info);
                passCheck = true;
            }
        }
        else
        {
            //TODO ������Ҫ���ƵĹ���
        }
        return passCheck;
    }

    private void CreateFurDataTex()
    {
        string furDataTexName = string.Empty;
        string furDataTexFolder = "Assets/Textures/FurDataTex/";
        Texture2D furDataTex = new Texture2D(512, 512, TextureFormat.ARGB32, false, true);
        for (int index = 1; index <= 100; index++)
        {
            if (!File.Exists(furDataTexFolder + Selection.activeTransform.name + ".png"))
            {
                furDataTexName = Selection.activeTransform.name;
                break;
            }
            string next = Selection.activeTransform.name + "_" + index;
            if (!File.Exists(furDataTexFolder + next + ".png"))
            {
                furDataTexName = next;
                break;
            }
        }
        string path = furDataTexFolder + furDataTexName + ".png";
        byte[] bytes = furDataTex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter textureIm = AssetImporter.GetAtPath(path) as TextureImporter;
        textureIm.textureCompression = TextureImporterCompression.Compressed;
        textureIm.isReadable = true;
        textureIm.mipmapEnabled = false;
        textureIm.wrapMode = TextureWrapMode.Clamp;
        textureIm.sRGBTexture = false;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}
