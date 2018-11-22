using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ObjParse
{
    /// <summary>
    /// 解析obj文件
    /// </summary>
    /// <param name="objPath"></param>
    /// <returns></returns>
    public static ObjModel ParseObj(string objContent,string objName = "obj")
    {
        if (string.IsNullOrEmpty(objContent))
            return null;

        ObjModel model = new ObjModel(objName);
        string matName = "Standard";
        string[] allLines = objContent.Split('\n');
        //将文本化后的obj文件内容按行分割        
        foreach (string line in allLines)
        {
            if (string.IsNullOrEmpty(line) || line[0] == '#' || string.IsNullOrEmpty(line))
                continue;

            //去掉多余的空格 有的中间会是2个空格
            string temp = line.Trim().Replace("  ", " ");
            string[] chars = temp.Split(' ');
            if (chars.Length <= 1)
                continue;

            //移除第一个空格前的字符   如 v f vt vn
            string data = temp.Remove(0, temp.IndexOf(' ') + 1);
            //根据第一个字符来判断数据的类型
            switch (chars[0])
            {
                case "mtllib":
                    model.MtlName = data;
                    break;
                //处理顶点
                case "v":
                    model.VertexList.Add(StringToVector3(chars));
                    break;
                //处理法线
                case "vn":                    
                    model.NormalList.Add(StringToVector3(chars));
                    break;
                //处理UV
                case "vt":
                    model.UVList.Add(StringToVector3(chars));
                    break;
                case "g":
                case "o":
                    model.AddObjPart(data);
                    break;
                //使用的材质球名字
                case "usemtl":
                    matName = data;
                    break;
                //处理面
                case "f":
                    if (model.LastPart == null)
                        continue;

                    //3个点
                    if(chars.Length >= 4)
                    {
                        string[] faceStr = new string[] { chars[1], chars[2], chars[3] };
                        ObjFace face = new ObjFace(matName, faceStr);
                        model.LastPart.FaceList.Add(face);
                    }
                    //4个点  相当于2个面
                    if(chars.Length >= 5)
                    {
                        string[] faceStr = new string[] { chars[3], chars[4], chars[1] };
                        ObjFace face = new ObjFace(matName, faceStr);
                        model.LastPart.FaceList.Add(face);
                    }

                    if (model.LastPart.MatsList.Contains(matName) == false)
                        model.LastPart.MatsList.Add(matName);
                    break;
            }
        }

        return model;
    }

    /// <summary>
    /// 解析材质文件
    /// </summary>
    /// <param name="path"></param>
    public static Dictionary<string,Material> ParseMtl(string mtlContent)
    {
        Dictionary<string, Material> matsDict = new Dictionary<string, Material>();
        if (string.IsNullOrEmpty(mtlContent))
            return matsDict;

        Material currentMaterial = null;
        string[] allLines = mtlContent.Split('\n');
        if (allLines.Length <= 0)
            return matsDict;

        foreach (string line in allLines)
        {
            string l = line.Trim().Replace("  ", " ");
            string[] chars = l.Split(' ');
            string data = l.Remove(0, l.IndexOf(' ') + 1);
            if (chars[0] == "newmtl")
            {
                currentMaterial = new Material(Shader.Find("Standard"));
                currentMaterial.name = data;
                if (matsDict.ContainsKey(data) == false)
                    matsDict.Add(data, currentMaterial);
            }
            else if (chars[0] == "Kd")
            {
                currentMaterial.SetColor("_Color", StringToColor(chars));
            }
            else if (chars[0] == "map_Kd")
            {
                //data 图片名字
                //TEXTURE
                //currentMaterial.SetTexture("_MainTex");
            }
            else if (chars[0] == "map_Bump")
            {
                //data 图片名字
                //TEXTURE
                //currentMaterial.SetTexture("_BumpMap");
                //currentMaterial.EnableKeyword("_NORMALMAP");
            }
            else if (chars[0] == "Ks")
            {
                currentMaterial.SetColor("_SpecColor", StringToColor(chars));
            }
            else if (chars[0] == "Ka")
            {
                currentMaterial.SetColor("_EmissionColor", StringToColor(chars, 0.05f));
                currentMaterial.EnableKeyword("_EMISSION");
            }
            else if (chars[0] == "d")
            {
                float visibility = float.Parse(chars[1]);
                if (visibility < 1)
                {
                    Color temp = currentMaterial.color;

                    temp.a = visibility;
                    currentMaterial.SetColor("_Color", temp);

                    //TRANSPARENCY ENABLER
                    currentMaterial.SetFloat("_Mode", 3);
                    currentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    currentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    currentMaterial.SetInt("_ZWrite", 0);
                    currentMaterial.DisableKeyword("_ALPHATEST_ON");
                    currentMaterial.EnableKeyword("_ALPHABLEND_ON");
                    currentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    currentMaterial.renderQueue = 3000;
                }

            }
            else if (chars[0] == "Ns")
            {
                float Ns = float.Parse(chars[1]);
                Ns = (Ns / 1000);
                currentMaterial.SetFloat("_Glossiness", Ns);
            }
        }
        return matsDict;
    }

    /// <summary>
    /// 字符转vecotr3
    /// </summary>
    /// <param name="chars"></param>
    /// <returns></returns>
    private static Vector3 StringToVector3(string[] chars)
    {
        if (chars.Length <= 2)
            return Vector2.zero;
        float x = float.Parse(chars[1]);
        float y = float.Parse(chars[2]);
        if (chars.Length == 3)
            return new Vector2(x, y);
        
        float z = float.Parse(chars[3]);
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmps"></param>
    /// <param name="scalar"></param>
    /// <returns></returns>
    private static Color StringToColor(string[] chars, float scalar = 1.0f)
    {
        float Kr = float.Parse(chars[1]) * scalar;
        float Kg = float.Parse(chars[2]) * scalar;
        float Kb = float.Parse(chars[3]) * scalar;
        return new Color(Kr, Kg, Kb);
    }
}
