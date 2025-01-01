using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AOT;
using Unity.VisualScripting;
using TMPro;
using System.Runtime.Serialization.Json;
using Unity.VisualScripting.FullSerializer;
using Palmmedia.ReportGenerator.Core.Common;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using UnityEngine.TestTools;
using UnityEditor.Overlays;

public class CustomLevelImport : EditorWindow
{
    public bool remixAlbedoAlpha = false;
    public bool remixOcclusionAndPBR = false;

    [MenuItem("Custom Editor Tools/Custom Level Json Import")]
    public static void ShowWindow()
    {
        //get the window and open it
        GetWindow(typeof(CustomLevelImport));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Open Custom Level Json"))
            ImportCustomLevel();
    }

    private static Texture2D RemixAlbedoWithAlpha(Texture2D albedo, Texture2D alpha, string savedPath)
    {
        Texture2D albedoReadable = new Texture2D(albedo.width, albedo.height, albedo.format, true);
        Texture2D alphaReadable = new Texture2D(alpha.width, alpha.height, alpha.format, true);
        Graphics.CopyTexture(albedo, albedoReadable);
        Graphics.CopyTexture(alpha, alphaReadable);
        albedoReadable.Apply();
        alphaReadable.Apply();

        Texture2D newAlbedo = new Texture2D(albedo.width, albedo.height, TextureFormat.ARGB32, true);
        Color[] originalAlbedoColors = albedoReadable.GetPixels();
        Color[] originalAlphaColors = alphaReadable.GetPixels();
        Color[] newAlbedoColors = new Color[originalAlbedoColors.Length];

        for (int i = 0; i < newAlbedoColors.Length; i++)
            newAlbedoColors[i] = new Color(originalAlbedoColors[i].r, originalAlbedoColors[i].g, originalAlbedoColors[i].b, originalAlphaColors[i].r);

        newAlbedo.SetPixels(newAlbedoColors);
        newAlbedo.Apply();

        AssetDatabase.CreateAsset(newAlbedo, savedPath);

        DestroyImmediate(albedoReadable);
        DestroyImmediate(alphaReadable);

        return newAlbedo;
    }

    private static Texture2D RemixPBR_ForHDRP(Texture2D pbrMap, Texture2D occlusion, string savedPath)
    {
        /*
        if(pbrMap != null && occlusion != null)
        {
            Texture2D pbrMapReadable = new Texture2D(pbrMap.width, pbrMap.height, pbrMap.format, pbrMap.mipmapCount > 1);
            Texture2D occlusionReadable = new Texture2D(occlusion.width, occlusion.height, occlusion.format, occlusion.mipmapCount > 1);
            Graphics.CopyTexture(pbrMap, pbrMapReadable);
            Graphics.CopyTexture(occlusion, occlusionReadable);
            pbrMapReadable.Apply();
            occlusionReadable.Apply();

            Texture2D newPBR = new Texture2D(pbrMap.width, pbrMap.height, TextureFormat.ARGB32, true);
            Color[] originalPBRColors = pbrMapReadable.GetPixels(0); //FF7R Mapping (MRV = Metallic | Roughness | Variant)
            Color[] originalOcclusionColors = occlusionReadable.GetPixels(0);
            Color[] newPBRColors = new Color[originalPBRColors.Length];

            //HDRP MAPPING: R = METALLIC | G = AO | B = DETAIL MASK | A = SMOOTHNESS
            for (int i = 0; i < newPBRColors.Length; i++)
                newPBRColors[i] = new Color(originalPBRColors[i].r, originalOcclusionColors[i].r, 0, 1 - Mathf.Sqrt(originalPBRColors[i].g));

            newPBR.SetPixels(newPBRColors);
            newPBR.Apply(true);

            AssetDatabase.CreateAsset(newPBR, savedPath);

            DestroyImmediate(pbrMapReadable);
            DestroyImmediate(occlusionReadable);

            return newPBR;
        }
        */
        //else if(pbrMap != null && occlusion == null)
        if (pbrMap != null)
        {
            Texture2D pbrMapReadable = new Texture2D(pbrMap.width, pbrMap.height, pbrMap.format, pbrMap.mipmapCount > 1);
            Graphics.CopyTexture(pbrMap, pbrMapReadable);
            pbrMapReadable.Apply();

            Texture2D newPBR = new Texture2D(pbrMap.width, pbrMap.height, TextureFormat.ARGB32, true);
            Color[] originalPBRColors = pbrMapReadable.GetPixels(0); //FF7R Mapping (MRV = Metallic | Roughness | Variant)
            Color[] newPBRColors = new Color[originalPBRColors.Length];

            //HDRP MAPPING: R = METALLIC | G = AO | B = DETAIL MASK | A = SMOOTHNESS
            for (int i = 0; i < newPBRColors.Length; i++)
                newPBRColors[i] = new Color(originalPBRColors[i].r, 0, 0, 1 - Mathf.Sqrt(originalPBRColors[i].g));

            newPBR.SetPixels(newPBRColors);
            newPBR.Apply(true);

            AssetDatabase.CreateAsset(newPBR, savedPath);

            DestroyImmediate(pbrMapReadable);

            return newPBR;
        }

        return null;
    }

    public static string SubstringBeforeLast(string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
    {
        var index = s.LastIndexOf(delimiter, comparisonType);
        return index == -1 ? s : s.Substring(0, index);
    }

    private static void CreateFilePath(string pathRelativeToUnityProject)
    {
        string[] folders = pathRelativeToUnityProject.Split('/');
        string currentPath = folders[0] + "/";

        for (int i = 1; i < folders.Length; i++)
        {
            currentPath += folders[i];

            if (AssetDatabase.IsValidFolder(currentPath) == false)
                AssetDatabase.CreateFolder(SubstringBeforeLast(currentPath, "/"), folders[i]);

            currentPath += "/";
        }

    }

    private static string ReformatPathStringWithForwardSlashes(string initalString)
    {
        string reformattedString = initalString.Replace("\\", "/").Replace("//", "/");
        return Regex.Replace(reformattedString, "/{2,}", "/");
    }

    private static string ExtractParentFolderOfExportedFilePath(string exportedFilePath, string referencePackagePath)
    {
        string reformattedExportedFilePath = ReformatPathStringWithForwardSlashes(exportedFilePath);
        string reformattedReferencePackagePath = ReformatPathStringWithForwardSlashes(referencePackagePath);
        string[] reformattedExportedFilePathSplit = reformattedExportedFilePath.Split("/");
        string[] reformattedReferencePackagePathSplit = reformattedReferencePackagePath.Split("/");
        string result = "";

        for(int i = 0; i < reformattedExportedFilePathSplit.Length; i++)
        {
            if (reformattedExportedFilePathSplit[i] != reformattedReferencePackagePathSplit[0])
                result += reformattedExportedFilePathSplit[i] + "/";
            else
                break;
        }

        result = SubstringBeforeLast(result, "/");

        return result;
    }

    private static void ImportCustomLevel()
    {
        string path = EditorUtility.OpenFilePanel("Open Level", "", "json");

        if (path.Length <= 0)
            return;

        ExportedLevel exportedLevel = new ExportedLevel();

        using (StreamReader file = File.OpenText(path))
        {
            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

            exportedLevel = (ExportedLevel)serializer.Deserialize(file, typeof(ExportedLevel));

            // Deserialize into a List<object> first or directly into a known type
            //var jsonObjects = (List<object>)serializer.Deserialize(file, typeof(List<object>));

            // Extract individual lists from the deserialized data
            //exportedLevel.LightActors = JsonConvert.DeserializeObject<List<ExtractedLightActor>>(jsonObjects[0].ToString());
            //exportedLevel.MeshActors = JsonConvert.DeserializeObject<List<ExtractedMeshActor>>(jsonObjects[1].ToString());
            //exportedLevel.ExportedMeshReferences = JsonConvert.DeserializeObject<List<ExportedFile>>(jsonObjects[2].ToString());
            //exportedLevel.ExportedTextureReferences = JsonConvert.DeserializeObject<List<ExportedFile>>(jsonObjects[3].ToString());
        }

        string umapLevelName = Path.GetFileNameWithoutExtension(path);
        string umapLevelBaseFolderPath = string.Format("Assets/{0}", umapLevelName);

        string unityAssetsFolderPath = Application.dataPath;
        string unityAssetsParentFolder = SubstringBeforeLast(unityAssetsFolderPath, "/");

        //|||||||||||||||||||||||||||||||||||||||| IMPORT MESH ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| IMPORT MESH ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| IMPORT MESH ||||||||||||||||||||||||||||||||||||||||

        for (int i = 0; i < exportedLevel.ExportedMeshReferences.Count; i++)
        {
            ExportedFile exportedMeshFile = exportedLevel.ExportedMeshReferences[i];

            string meshSystemPath = ReformatPathStringWithForwardSlashes(exportedMeshFile.ExportedFilePath);
            string meshSystemRelativeFolder = ExtractParentFolderOfExportedFilePath(meshSystemPath, exportedMeshFile.PackagePathReference);

            Debug.Log(meshSystemPath);
            Debug.Log(meshSystemRelativeFolder);

            string meshSystemPathWithoutRelativeFolder = "";

            try
            {
                meshSystemPathWithoutRelativeFolder = meshSystemPath.Remove(0, meshSystemRelativeFolder.Length + 1);
            }
            catch(Exception e)
            {
                continue;
            }


            //return;

            string meshNewCopiedSystemPath = string.Format("{0}/{1}", unityAssetsFolderPath, meshSystemPathWithoutRelativeFolder);
            string meshNewCopiedSystemPathRelative = string.Format("Assets/{0}", meshSystemPathWithoutRelativeFolder);

            CreateFilePath(SubstringBeforeLast(meshNewCopiedSystemPathRelative, "/"));

            if (File.Exists(meshSystemPath) == false)
            {
                Debug.LogWarning(string.Format("exported file for mesh package {0} does not have a converted mesh! {1}", exportedMeshFile.PackagePathReference, meshSystemPath));
                continue;
            }

            if(File.Exists(meshNewCopiedSystemPath) == false)
            {
                File.Copy(meshSystemPath, meshNewCopiedSystemPath);
            }
        }

        //|||||||||||||||||||||||||||||||||||||||| IMPORT TEXTURES ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| IMPORT TEXTURES ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| IMPORT TEXTURES ||||||||||||||||||||||||||||||||||||||||

        for (int i = 0; i < exportedLevel.ExportedTextureReferences.Count; i++)
        {
            ExportedFile exportedTextureFile = exportedLevel.ExportedTextureReferences[i];
            string systemPath = ReformatPathStringWithForwardSlashes(exportedTextureFile.ExportedFilePath);
            string packagePath = SubstringBeforeLast(exportedTextureFile.PackagePathReference, "/");

            CreateFilePath(string.Format("Assets/{0}", packagePath));

            string assetFileName = Path.GetFileName(systemPath);

            string newCopiedSystemPath = string.Format("{0}/{1}/{2}", unityAssetsFolderPath, packagePath, assetFileName);
            string newCopiedSystemPathRelative = string.Format("Assets/{0}/{1}", packagePath, assetFileName);

            if (File.Exists(systemPath) == false)
            {
                Debug.LogWarning(string.Format("exported file for texture package {0} does not have a converted texture! {1}", exportedTextureFile.PackagePathReference, systemPath));
                continue;
            }
            
            if (File.Exists(newCopiedSystemPath) == false)
            {
                File.Copy(systemPath, newCopiedSystemPath);
            }

            EditorUtility.DisplayProgressBar("Level Import", "Importing Textures...", (1.0f / exportedLevel.ExportedMeshReferences.Count) * i);
        }

        EditorUtility.ClearProgressBar();

        AssetDatabase.Refresh();

        //|||||||||||||||||||||||||||||||||||||||| CREATE MATERIALS ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| CREATE MATERIALS ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| CREATE MATERIALS ||||||||||||||||||||||||||||||||||||||||

        Material fallbackMaterial = GetMaterialFallback("Assets/");

        for (int i = 0; i < exportedLevel.MeshActors.Count; i++)
        {
            ExtractedMeshActor extractedMeshActor = exportedLevel.MeshActors[i];
            ExportedFile exportedMeshFile = exportedLevel.ExportedMeshReferences[i];

            string meshSystemPath = ReformatPathStringWithForwardSlashes(exportedMeshFile.ExportedFilePath);
            string meshSystemRelativeFolder = ExtractParentFolderOfExportedFilePath(meshSystemPath, exportedMeshFile.PackagePathReference);

            string meshSystemPathRelativeToAssets = "";

            try
            {
                meshSystemPathRelativeToAssets = string.Format("Assets/{0}", meshSystemPath.Remove(0, meshSystemRelativeFolder.Length + 1));
            }
            catch (Exception e)
            {
                continue;
            }

            for (int j = 0; j < extractedMeshActor.MaterialReferences.Count; j++)
            {
                MaterialReference materialReference = extractedMeshActor.MaterialReferences[j];

                string materialSavePath = string.Format("{0}/{1}.mat", SubstringBeforeLast(meshSystemPathRelativeToAssets, "/"), materialReference.Name);

                Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialSavePath);

                if (newMaterial == null)
                {
                    newMaterial = new Material(Shader.Find("HDRP/Lit"));
                    AssetDatabase.CreateAsset(newMaterial, materialSavePath);
                }

                newMaterial.shader = Shader.Find("HDRP/Lit");

                //hdrp lit
                newMaterial.SetColor("_BaseColor", Color.white);
                newMaterial.SetFloat("_Smoothness", 0.0f);
                newMaterial.SetFloat("_Metallic", 0.0f);
                newMaterial.SetFloat("_AORemapMin", 0.0f);
                newMaterial.SetFloat("_AORemapMax", 0.0f);
                newMaterial.name = materialReference.Name;

                TextureReference mainColorTextureReference = GetTextureReferenceByMaterialParameterName("0:0:Color", materialReference.TextureReferences);
                TextureReference alphaColorTextureReference = GetTextureReferenceByMaterialParameterName("0:1:Coverage", materialReference.TextureReferences);
                TextureReference pbrColorTextureReference = GetTextureReferenceByMaterialParameterName("0:2:Metallic/Roughness/Variant", materialReference.TextureReferences);
                TextureReference normalColorTextureReference = GetTextureReferenceByMaterialParameterName("0:3:Normal", materialReference.TextureReferences);
                TextureReference emissiveColorTextureReference = GetTextureReferenceByMaterialParameterName("0:4:Emissive", materialReference.TextureReferences);
                TextureReference occlusionColorTextureReference = GetTextureReferenceByMaterialParameterName("1:0:Occlusion", materialReference.TextureReferences);

                /*
                if (mainColorTextureReference != null && alphaColorTextureReference != null)
                {
                    ExportedFile mainColorExportedFileReference = GetExportedFileByPackagePath(mainColorTextureReference.PackagePath, exportedLevel.ExportedTextureReferences);
                    ExportedFile alphaColorExportedFileReference = GetExportedFileByPackagePath(alphaColorTextureReference.PackagePath, exportedLevel.ExportedTextureReferences);

                    if (mainColorExportedFileReference != null && alphaColorExportedFileReference != null)
                    {
                        string mainColorTextureSystemPath = ReformatPathStringWithForwardSlashes(mainColorExportedFileReference.ExportedFilePath);
                        string mainColorTexturePackagePath = SubstringBeforeLast(mainColorExportedFileReference.PackagePathReference, "/");
                        string mainColorTextureFileName = Path.GetFileName(mainColorTextureSystemPath);
                        string mainColorTextureRelativePath = string.Format("Assets/{0}/{1}", mainColorTexturePackagePath, mainColorTextureFileName);
                        Texture2D mainColor = AssetDatabase.LoadAssetAtPath<Texture2D>(mainColorTextureRelativePath);

                        string alphaColorTextureSystemPath = ReformatPathStringWithForwardSlashes(alphaColorExportedFileReference.ExportedFilePath);
                        string alphaColorTexturePackagePath = SubstringBeforeLast(alphaColorExportedFileReference.PackagePathReference, "/");
                        string alphaColorTextureFileName = Path.GetFileName(alphaColorTextureSystemPath);
                        string alphaColorTextureRelativePath = string.Format("Assets/{0}/{1}", alphaColorTexturePackagePath, alphaColorTextureFileName);
                        Texture2D alphaColor = AssetDatabase.LoadAssetAtPath<Texture2D>(alphaColorTextureRelativePath);

                        string newMainColorTextureRelativePath = string.Format("Assets/{0}/{1}.asset", mainColorTexturePackagePath, Path.GetFileNameWithoutExtension(mainColorTextureSystemPath));

                        if(mainColor != null && alphaColor != null)
                        {
                            Texture2D remixedMainColor = RemixAlbedoWithAlpha(mainColor, alphaColor, newMainColorTextureRelativePath);

                            //hdrp lit
                            newMaterial.SetTexture("_BaseColorMap", remixedMainColor);
                            newMaterial.SetFloat("_AlphaCutoffEnable", 1);
                            newMaterial.SetFloat("_AlphaCutoff", 0.5f);
                        }
                    }
                    else if(mainColorExportedFileReference != null && alphaColorExportedFileReference == null)
                    {
                        string mainColorTextureSystemPath = ReformatPathStringWithForwardSlashes(mainColorExportedFileReference.ExportedFilePath);
                        string mainColorTexturePackagePath = SubstringBeforeLast(mainColorExportedFileReference.PackagePathReference, "/");
                        string mainColorTextureFileName = Path.GetFileName(mainColorTextureSystemPath);
                        string mainColorTextureRelativePath = string.Format("Assets/{0}/{1}", mainColorTexturePackagePath, mainColorTextureFileName);
                        Texture2D mainColor = AssetDatabase.LoadAssetAtPath<Texture2D>(mainColorTextureRelativePath);

                        if (mainColor != null)
                        {
                            //hdrp lit
                            newMaterial.SetTexture("_BaseColorMap", mainColor);
                        }
                    }
                }
                else if (mainColorTextureReference != null && alphaColorTextureReference == null)
                {
                    ExportedFile mainColorExportedFileReference = GetExportedFileByPackagePath(mainColorTextureReference.PackagePath, exportedLevel.ExportedTextureReferences);

                    if (mainColorExportedFileReference != null)
                    {
                        string mainColorTextureSystemPath = ReformatPathStringWithForwardSlashes(mainColorExportedFileReference.ExportedFilePath);
                        string mainColorTexturePackagePath = SubstringBeforeLast(mainColorExportedFileReference.PackagePathReference, "/");
                        string mainColorTextureFileName = Path.GetFileName(mainColorTextureSystemPath);
                        string mainColorTextureRelativePath = string.Format("Assets/{0}/{1}", mainColorTexturePackagePath, mainColorTextureFileName);
                        Texture2D mainColor = AssetDatabase.LoadAssetAtPath<Texture2D>(mainColorTextureRelativePath);

                        if (mainColor != null)
                        {
                            //hdrp lit
                            newMaterial.SetTexture("_BaseColorMap", mainColor);
                        }
                    }
                }
                */

                if (mainColorTextureReference != null)
                {
                    ExportedFile mainColorExportedFileReference = GetExportedFileByPackagePath(mainColorTextureReference.PackagePath, exportedLevel.ExportedTextureReferences);

                    if (mainColorExportedFileReference != null)
                    {
                        string mainColorTextureSystemPath = ReformatPathStringWithForwardSlashes(mainColorExportedFileReference.ExportedFilePath);
                        string mainColorTexturePackagePath = SubstringBeforeLast(mainColorExportedFileReference.PackagePathReference, "/");
                        string mainColorTextureFileName = Path.GetFileName(mainColorTextureSystemPath);
                        string mainColorTextureRelativePath = string.Format("Assets/{0}/{1}", mainColorTexturePackagePath, mainColorTextureFileName);
                        Texture2D mainColor = AssetDatabase.LoadAssetAtPath<Texture2D>(mainColorTextureRelativePath);

                        if (mainColor != null)
                        {
                            //hdrp lit
                            newMaterial.SetTexture("_BaseColorMap", mainColor);
                        }
                    }
                }

                if (normalColorTextureReference != null)
                {
                    ExportedFile normalColorExportedFileReference = GetExportedFileByPackagePath(normalColorTextureReference.PackagePath, exportedLevel.ExportedTextureReferences);

                    if (normalColorExportedFileReference != null)
                    {
                        string normalColorTextureSystemPath = ReformatPathStringWithForwardSlashes(normalColorExportedFileReference.ExportedFilePath);
                        string normalColorTexturePackagePath = SubstringBeforeLast(normalColorExportedFileReference.PackagePathReference, "/");
                        string normalColorTextureFileName = Path.GetFileName(normalColorTextureSystemPath);
                        string normalColorTextureRelativePath = string.Format("Assets/{0}/{1}", normalColorTexturePackagePath, normalColorTextureFileName);
                        Texture2D normalColor = AssetDatabase.LoadAssetAtPath<Texture2D>(normalColorTextureRelativePath);

                        if (normalColor != null)
                        {
                            //hdrp lit
                            newMaterial.SetTexture("_NormalMap", normalColor);
                        }
                    }
                }

                if (emissiveColorTextureReference != null)
                {
                    ExportedFile emissiveColorExportedFileReference = GetExportedFileByPackagePath(emissiveColorTextureReference.PackagePath, exportedLevel.ExportedTextureReferences);

                    if (emissiveColorExportedFileReference != null)
                    {
                        string emissiveColorTextureSystemPath = ReformatPathStringWithForwardSlashes(emissiveColorExportedFileReference.ExportedFilePath);
                        string emissiveColorTexturePackagePath = SubstringBeforeLast(emissiveColorExportedFileReference.PackagePathReference, "/");
                        string emissiveColorTextureFileName = Path.GetFileName(emissiveColorTextureSystemPath);
                        string emissiveColorTextureRelativePath = string.Format("Assets/{0}/{1}", emissiveColorTexturePackagePath, emissiveColorTextureFileName);
                        Texture2D emissiveColor = AssetDatabase.LoadAssetAtPath<Texture2D>(emissiveColorTextureRelativePath);

                        if (emissiveColor != null)
                        {
                            //hdrp lit
                            newMaterial.SetTexture("_EmissiveColorMap", emissiveColor);
                            newMaterial.SetColor("_EmissiveColor", Color.white);
                        }
                    }
                }

                /*
                if(pbrColorTextureReference != null && occlusionColorTextureReference != null)
                {
                    ExportedFile pbrColorExportedFileReference = GetExportedFileByPackagePath(pbrColorTextureReference.PackagePath, parentContainer.exportedTexturePackagePathReferences);
                    ExportedFile occlusionColorExportedFileReference = GetExportedFileByPackagePath(occlusionColorTextureReference.PackagePath, parentContainer.exportedTexturePackagePathReferences);

                    if(pbrColorExportedFileReference != null && occlusionColorExportedFileReference != null)
                    {
                        string pbrColorTextureSystemPath = ReformatPathStringWithForwardSlashes(pbrColorExportedFileReference.ExportedFilePath);
                        string pbrColorTexturePackagePath = SubstringBeforeLast(pbrColorExportedFileReference.PackagePathReference, "/");
                        string pbrColorTextureFileName = Path.GetFileName(pbrColorTextureSystemPath);
                        string pbrColorTextureRelativePath = string.Format("Assets/{0}/{1}", pbrColorTexturePackagePath, pbrColorTextureFileName);
                        Texture2D pbrColor = AssetDatabase.LoadAssetAtPath<Texture2D>(pbrColorTextureRelativePath);

                        string occlusionColorTextureSystemPath = ReformatPathStringWithForwardSlashes(occlusionColorExportedFileReference.ExportedFilePath);
                        string occlusionColorTexturePackagePath = SubstringBeforeLast(occlusionColorExportedFileReference.PackagePathReference, "/");
                        string occlusionColorTextureFileName = Path.GetFileName(occlusionColorTextureSystemPath);
                        string occlusionColorTextureRelativePath = string.Format("Assets/{0}/{1}", occlusionColorTexturePackagePath, occlusionColorTextureFileName);
                        Texture2D occlusionColor = AssetDatabase.LoadAssetAtPath<Texture2D>(occlusionColorTextureRelativePath);

                        string newPBRColorTextureRelativePath = string.Format("Assets/{0}/{1}.asset", pbrColorTexturePackagePath, Path.GetFileNameWithoutExtension(pbrColorTextureSystemPath));

                        if (pbrColor != null && occlusionColor != null)
                        {
                            Texture2D remixedPBRColor = RemixPBR_ForHDRP(pbrColor, occlusionColor, newPBRColorTextureRelativePath);

                            //hdrp lit
                            newMaterial.SetTexture("_MaskMap", remixedPBRColor);
                            newMaterial.SetFloat("_Smoothness", 1.0f);
                            newMaterial.SetFloat("_Metallic", 1.0f);
                            newMaterial.SetFloat("_AORemapMin", 0.0f);
                            newMaterial.SetFloat("_AORemapMax", 0.0f);
                        }
                    }
                }
                else if (pbrColorTextureReference != null && occlusionColorTextureReference == null)
                {
                    ExportedFile pbrColorExportedFileReference = GetExportedFileByPackagePath(pbrColorTextureReference.PackagePath, parentContainer.exportedTexturePackagePathReferences);

                    if (pbrColorExportedFileReference != null)
                    {
                        string pbrColorTextureSystemPath = ReformatPathStringWithForwardSlashes(pbrColorExportedFileReference.ExportedFilePath);
                        string pbrColorTexturePackagePath = SubstringBeforeLast(pbrColorExportedFileReference.PackagePathReference, "/");
                        string pbrColorTextureFileName = Path.GetFileName(pbrColorTextureSystemPath);
                        string pbrColorTextureRelativePath = string.Format("Assets/{0}/{1}", pbrColorTexturePackagePath, pbrColorTextureFileName);
                        Texture2D pbrColor = AssetDatabase.LoadAssetAtPath<Texture2D>(pbrColorTextureRelativePath);

                        string newPBRColorTextureRelativePath = string.Format("Assets/{0}/{1}.asset", pbrColorTexturePackagePath, Path.GetFileNameWithoutExtension(pbrColorTextureSystemPath));

                        if (pbrColor != null)
                        {
                            Texture2D remixedPBRColor = RemixPBR_ForHDRP(pbrColor, null, newPBRColorTextureRelativePath);

                            //hdrp lit
                            newMaterial.SetTexture("_MaskMap", remixedPBRColor);
                            newMaterial.SetFloat("_Smoothness", 1.0f);
                            newMaterial.SetFloat("_Metallic", 1.0f);
                            newMaterial.SetFloat("_AORemapMin", 0.0f);
                            newMaterial.SetFloat("_AORemapMax", 0.0f);
                        }
                    }
                }
                */

                if (pbrColorTextureReference != null)
                {
                    ExportedFile pbrColorExportedFileReference = GetExportedFileByPackagePath(pbrColorTextureReference.PackagePath, exportedLevel.ExportedTextureReferences);

                    if (pbrColorExportedFileReference != null)
                    {
                        string pbrColorTextureSystemPath = ReformatPathStringWithForwardSlashes(pbrColorExportedFileReference.ExportedFilePath);
                        string pbrColorTexturePackagePath = SubstringBeforeLast(pbrColorExportedFileReference.PackagePathReference, "/");
                        string pbrColorTextureFileName = Path.GetFileName(pbrColorTextureSystemPath);
                        string pbrColorTextureRelativePath = string.Format("Assets/{0}/{1}", pbrColorTexturePackagePath, pbrColorTextureFileName);
                        Texture2D pbrColor = AssetDatabase.LoadAssetAtPath<Texture2D>(pbrColorTextureRelativePath);

                        string newPBRColorTextureRelativePath = string.Format("Assets/{0}/{1}.asset", pbrColorTexturePackagePath, Path.GetFileNameWithoutExtension(pbrColorTextureSystemPath));

                        if (pbrColor != null)
                        {
                            //Texture2D remixedPBRColor = RemixPBR_ForHDRP(pbrColor, null, newPBRColorTextureRelativePath);

                            //hdrp lit
                            newMaterial.SetTexture("_MaskMap", pbrColor);
                            //newMaterial.SetFloat("_Smoothness", 1.0f);
                            //newMaterial.SetFloat("_Metallic", 1.0f);
                            //newMaterial.SetFloat("_AORemapMin", 0.0f);
                            //newMaterial.SetFloat("_AORemapMax", 0.0f);
                        }
                    }
                }
            }
        }

        AssetDatabase.Refresh();

        //|||||||||||||||||||||||||||||||||||||||| CREATE GAME OBJECTS FROM EXTRACTED MESH ACTORS ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| CREATE GAME OBJECTS FROM EXTRACTED MESH ACTORS ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| CREATE GAME OBJECTS FROM EXTRACTED MESH ACTORS ||||||||||||||||||||||||||||||||||||||||

        GameObject parentGameObjectLevel = new GameObject(umapLevelName);

        for (int i = 0; i < exportedLevel.MeshActors.Count; i++)
        {
            ExtractedMeshActor extractedMeshActor = exportedLevel.MeshActors[i];
            ExportedFile meshReferenceFile = GetExportedFileByPackagePath(extractedMeshActor.MeshPackagePath, exportedLevel.ExportedMeshReferences);
            string meshSystemPath = ReformatPathStringWithForwardSlashes(meshReferenceFile.ExportedFilePath);
            string meshSystemRelativeFolder = ExtractParentFolderOfExportedFilePath(meshSystemPath, meshReferenceFile.PackagePathReference);




            string meshSystemPathRelativeToAssets = "";

            try
            {
                meshSystemPathRelativeToAssets = string.Format("Assets/{0}", meshSystemPath.Remove(0, meshSystemRelativeFolder.Length + 1));
            }
            catch (Exception e)
            {
                continue;
            }





            GameObject modelGameObjectAsset = AssetDatabase.LoadAssetAtPath<GameObject>(meshSystemPathRelativeToAssets);

            GameObject meshActorGameObject = Instantiate(modelGameObjectAsset);
            meshActorGameObject.name = extractedMeshActor.Name;
            meshActorGameObject.transform.position = new Vector3(extractedMeshActor.Position.X, extractedMeshActor.Position.Y, extractedMeshActor.Position.Z);
            meshActorGameObject.transform.rotation = new Quaternion(extractedMeshActor.Rotation.X, extractedMeshActor.Rotation.Y, extractedMeshActor.Rotation.Z, extractedMeshActor.Rotation.W);

            //turns out scale is wrong, ends up staying zero?
            //so we do a quick and dirty scale fix, preferable to get actual scales during parsing but the unit seems to be 100
            meshActorGameObject.transform.localScale = Vector3.one * 100.0f;

            //correct all actor orientations, because orientations in UE are different
            meshActorGameObject.transform.rotation *= Quaternion.Euler(90.0f, 0, 0);
            meshActorGameObject.transform.rotation *= Quaternion.Euler(0, 180, 0);
            meshActorGameObject.transform.SetParent(parentGameObjectLevel.transform);

            //assign materials
            for (int index1 = 0; index1 < extractedMeshActor.MaterialReferences.Count; index1++)
            {
                //NOTE TO SELF: the exported GLB meshes have an awkward setup where instead of multiple submeshes being on the same mesh... they are on seperate child objects
                //so index 0 of submesh is the current mesh actor gameobject we are on, and any other indexes are child objects of the current mesh actor
                GameObject currentSubmesh = meshActorGameObject;

                if(index1 > 0)
                    currentSubmesh = meshActorGameObject.transform.GetChild(index1 - 1).gameObject;

                MaterialReference materialReference = extractedMeshActor.MaterialReferences[index1];

                string meshParentFolderPath = SubstringBeforeLast(meshSystemPathRelativeToAssets, "/");
                string materialSavePath = string.Format("{0}/{1}.mat", meshParentFolderPath, materialReference.Name);

                Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialSavePath);

                MeshRenderer meshRenderer = currentSubmesh.GetComponent<MeshRenderer>();

                if (newMaterial != null)
                    meshRenderer.sharedMaterial = newMaterial;
                else
                    meshRenderer.sharedMaterial = fallbackMaterial;
            }
        }

        //|||||||||||||||||||||||||||||||||||||||| CREATE GAME OBJECTS FROM EXTRACTED LIGHT ACTORS ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| CREATE GAME OBJECTS FROM EXTRACTED LIGHT ACTORS ||||||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||||||| CREATE GAME OBJECTS FROM EXTRACTED LIGHT ACTORS ||||||||||||||||||||||||||||||||||||||||

        for (int i = 0; i < exportedLevel.LightActors.Count; i++)
        {
            ExtractedLightActor extractedLightActor = exportedLevel.LightActors[i];

            GameObject lightActorGameObject = new GameObject(extractedLightActor.Name);
            lightActorGameObject.transform.position = new Vector3(extractedLightActor.Position.X, extractedLightActor.Position.Y, extractedLightActor.Position.Z);
            lightActorGameObject.transform.rotation = new Quaternion(extractedLightActor.Rotation.X, extractedLightActor.Rotation.Y, extractedLightActor.Rotation.Z, extractedLightActor.Rotation.W);

            //turns out scale is wrong, ends up staying zero?
            //so we do a quick and dirty scale fix, preferable to get actual scales during parsing but the unit seems to be 100
            lightActorGameObject.transform.localScale = Vector3.one * 100.0f;

            //correct all actor orientations, because orientations in UE are different
            lightActorGameObject.transform.rotation *= Quaternion.Euler(90.0f, 0, 0);
            lightActorGameObject.transform.rotation *= Quaternion.Euler(0, 180, 0);

            lightActorGameObject.transform.rotation *= Quaternion.Euler(0, -90.0f, 0);

            lightActorGameObject.transform.SetParent(parentGameObjectLevel.transform);

            //apply light component
            Light lightComponent = lightActorGameObject.AddComponent<Light>();

            switch(extractedLightActor.LightType)
            {
                case ExtractedLightType.Spot:
                    lightComponent.type = LightType.Spot;
                    break;
                case ExtractedLightType.Point:
                    lightComponent.type = LightType.Point; 
                    break;
                default:
                    lightComponent.enabled = false;
                    break;
            }

            //if (extractedLightActor.bUseTemperature)
            //lightComponent.color = Mathf.CorrelatedColorTemperatureToRGB(extractedLightActor.Temperature);
            //else

            if (extractedLightActor.LightColor.R == 0 && extractedLightActor.LightColor.G == 0 && extractedLightActor.LightColor.B == 0 && extractedLightActor.LightColor.A == 0)
                lightComponent.color = Color.white;
            else
                lightComponent.color = new Color((float)extractedLightActor.LightColor.R / 255.0f, (float)extractedLightActor.LightColor.G / 255.0f, (float)extractedLightActor.LightColor.B / 255.0f, (float)extractedLightActor.LightColor.A / 255.0f);

            lightComponent.intensity = extractedLightActor.Intensity;

            //if (extractedLightActor.IntensityUnits == "ELightUnits::Lumen (EnumProperty)")
            if (extractedLightActor.IntensityUnits == "ELightUnits::Candela (EnumProperty)")
                lightComponent.intensity = CandelaToLumens(extractedLightActor.Intensity, 120.0f);

            if (extractedLightActor.AttenuationRadius > 0)
                lightComponent.range = extractedLightActor.AttenuationRadius;

            if (extractedLightActor.SourceRadius > 0)
                lightComponent.shadowRadius = extractedLightActor.SourceRadius;

            if (extractedLightActor.OuterConeAngle > 0)
            {
                lightComponent.spotAngle = extractedLightActor.OuterConeAngle;

                if (extractedLightActor.InnerConeAngle > 0)
                    lightComponent.innerSpotAngle = extractedLightActor.InnerConeAngle;
            }
            else
            {
                if (extractedLightActor.InnerConeAngle > 0)
                    lightComponent.spotAngle = extractedLightActor.InnerConeAngle;
            }

            //if (extractedLightActor.CastShadows)
                lightComponent.shadows = LightShadows.Soft;
            //else
            //lightComponent.shadows = LightShadows.None;

            lightComponent.intensity *= 0.001f;
            lightComponent.range *= 0.001f;
            lightComponent.shadowRadius *= 0.001f;
            lightComponent.shadowBias = 0.0001f;
        }

        //make sure the entire level is upright
        parentGameObjectLevel.transform.rotation *= Quaternion.Euler(-90.0f, 0, 0);
        parentGameObjectLevel.transform.localScale *= 0.001f;
    }

    public static Material GetMaterialFallback(string baseDirectory)
    {
        string materialSavePath = string.Format("{0}/FALLBACK.mat", baseDirectory);

        Material fallbackMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialSavePath);

        if (fallbackMaterial == null)
        {
            //fallbackMaterial = new Material(Shader.Find("Standard"));
            fallbackMaterial = new Material(Shader.Find("HDRP/Lit"));
            AssetDatabase.CreateAsset(fallbackMaterial, materialSavePath);
        }

        fallbackMaterial.shader = Shader.Find("HDRP/Lit");

        //standard shader
        //fallbackMaterial.SetColor("_Color", Color.white);
        //fallbackMaterial.SetFloat("_Glossiness", 0.0f);
        //fallbackMaterial.SetFloat("_Metallic", 0.0f);

        //hdrp lit
        fallbackMaterial.SetColor("_BaseColor", Color.white);
        fallbackMaterial.SetFloat("_Smoothness", 0.0f);
        fallbackMaterial.SetFloat("_Metallic", 0.0f);
        fallbackMaterial.name = "FALLBACK";

        return fallbackMaterial;
    }

    public static ExportedFile GetExportedFileByPackagePath(string packagePath, List<ExportedFile> exportedFiles)
    {
        for(int i = 0; i < exportedFiles.Count; i++)
        {
            if (exportedFiles[i].PackagePathReference == packagePath)
                return exportedFiles[i];
        }

        return null;
    }

    public static TextureReference GetTextureReferenceByMaterialParameterName(string materialParameterName, List<TextureReference> textureReferences)
    {
        for (int i = 0; i < textureReferences.Count; i++)
        {
            if (textureReferences[i].MaterialParameterName == materialParameterName)
                return textureReferences[i];
        }

        return null;
    }

    /// <summary>
    /// Converts luminous intensity in candela to luminous flux in lumens.
    /// </summary>
    /// <param name="candela">Luminous intensity in candela (cd).</param>
    /// <param name="beamAngle">Beam angle in degrees.</param>
    /// <returns>Luminous flux in lumens (lm).</returns>
    public static float CandelaToLumens(float candela, float beamAngle)
    {
        // Convert beam angle from degrees to radians
        float theta = beamAngle * (Mathf.PI / 180.0f);

        // Calculate lumens using the formula
        float lumens = candela * 2 * Mathf.PI * (1 - Mathf.Cos(theta / 2));
        return lumens;
    }

    public class ExportedLevel
    {
        public List<ExtractedLightActor> LightActors;
        public List<ExtractedMeshActor> MeshActors;

        public List<ExportedFile> ExportedMeshReferences;
        public List<ExportedFile> ExportedTextureReferences;
    }

    public class FVector
    {
        public float X;
        public float Y;
        public float Z;
    }

    public class FQuat
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
        public bool IsNormalized;
        public float Size;
        public float SizeSquared;
    }

    public class FColor
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;
        public string Hex;
    }

    public enum ExtractedLightType
    {
        Spot,
        Point,
        Unknown
    }

    public class ExtractedLightActor
    {
        public string Name;

        public ExtractedLightType LightType;

        //transform
        public FVector Position;
        public FQuat Rotation;
        //public FVector Scale; //NOTE: scale might be needed for area lights? not sure

        //spot light
        public float OuterConeAngle;
        public float InnerConeAngle;

        public float AttenuationRadius; //light radius
        public float SourceRadius; //shadow radius/blur

        //common light properties
        public string IntensityUnits; //this isn't required per say, but it is helpful because it tells us the unit that the light intensity is at
        public float Intensity;
        public FColor LightColor;

        //specalized parameters to determine light color
        public bool bUseTemperature;
        public float Temperature;
        public string ColorTemperatureWhitePoint;

        public bool CastShadows;
    }

    public class ExtractedMeshActor
    {
        public string Name;

        public FVector Position;
        public FQuat Rotation;
        public FVector Scale;

        public string MeshPackagePath;
        public List<MaterialReference> MaterialReferences;
    }

    public class MaterialReference
    {
        public string Name;
        public List<TextureReference> TextureReferences;
    }

    public class TextureReference
    {
        public string Name;
        public string PackagePath;
        public string MaterialParameterName;
    }

    public class ExportedFile
    {
        public string PackagePathReference;
        public string ExportedFilePath;
    }
}
