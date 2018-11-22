using System.Collections.Generic;
using UnityEngine;

public class ObjPart
{
    /// <summary>
    /// 对象名称
    /// </summary>
    public string Name = "Obj";

    /// <summary>
    /// 材质名称
    /// </summary>
    public List<string> MatsList;

    public List<ObjFace> FaceList;


    public ObjPart(string name)
    {
        Name = name;
        MatsList = new List<string>();
        FaceList = new List<ObjFace>();
    }

    public GameObject GetObj(ObjModel model)
    {
        GameObject obj = new GameObject(Name);
        Mesh m = GetMesh(model);
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        Material[] materials = new Material[MatsList.Count];
        for(int i = 0;i<materials.Length;i++)
        {
            materials[i] = model.GetMaterial(MatsList[i]);
        }

        mr.materials = materials;
        mf.mesh = m;
        return obj;
    }

    private Mesh GetMesh(ObjModel model)
    {
        Mesh m = new Mesh
        {
            name = Name
        };
        int count = FaceList.Count * 3;
        List<Vector3> verticesList = new List<Vector3>();
        List<Vector3> normalList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();
        bool hasNormal = false;
        List<int[]> triangleList = new List<int[]>();
        //用于保存顶点索引（全局） 与当前mesh中 顶点的索引  
        Dictionary<string, int> indexDict = new Dictionary<string, int>();
        //按照材质去找面
        foreach (string matName in MatsList)
        {
            List<ObjFace> faces = FaceList.FindAll((ObjFace o) =>
            {
                if (o.MatName == matName) return true;
                return false;
            });

            int[] triangles = new int[faces.Count * 3];
            int idx = 0;
            //
            for (int i = 0; i < faces.Count; i++)
            {
                ObjFace face = faces[i];
                string[] f = face.FaceStr;

                for (int j = 0; j < f.Length; j++)
                {
                    string[] indexs = f[j].Split('/');
                    int vIndex = int.Parse(indexs[0]);
                    int nIndex = -1;
                    int uIndex = -1;

                    if (indexs.Length > 2 && indexs[2] != "")
                    {
                        nIndex = int.Parse(indexs[2]);
                        hasNormal = true;
                    }

                    //法线索引
                    if (indexs.Length > 1 && indexs[1] != "")
                    {
                        uIndex = int.Parse(indexs[1]);
                    }

                    string key = vIndex + "|" + uIndex + "|" + nIndex;
                    if (indexDict.ContainsKey(key))
                    {
                        triangles[idx] = indexDict[key];
                    }
                    else
                    {
                        triangles[idx] = verticesList.Count;
                        indexDict[key] = verticesList.Count;
                        verticesList.Add(model.VertexList[vIndex - 1]);
                        normalList.Add(nIndex == -1 ? Vector3.zero : model.NormalList[nIndex - 1]);
                        uvList.Add(uIndex == -1 ? Vector2.zero : model.UVList[uIndex - 1]);
                    }
                    idx++;
                }
            }
            triangleList.Add(triangles);
        }

        m.vertices = verticesList.ToArray();
        m.normals = normalList.ToArray();
        m.uv = uvList.ToArray();
        m.subMeshCount = triangleList.Count;
        for(int i = 0;i<triangleList.Count;i++)
            m.SetTriangles(triangleList[i],i); 

        if (!hasNormal)
        {
            m.RecalculateNormals();
        }
        m.RecalculateBounds();
        return m;
    }
}

/// <summary>
/// obj的面
/// </summary>
public class ObjFace
{
    public string MatName;
    public string[] FaceStr;

    public ObjFace(string matName,string[] faceStr)
    {
        MatName = matName;
        FaceStr = faceStr;
    }
}

/// <summary>
/// 将obj解析成对象
/// </summary>
public class ObjModel
{
    /// <summary>
    /// 存储材质球的文件名
    /// </summary>
    public string MtlName;

    /// <summary>
    /// UV坐标列表
    /// </summary>
    public List<Vector2> UVList;

    /// <summary>
    /// 法线列表
    /// </summary>
    public List<Vector3> NormalList;

    /// <summary>
    /// 顶点列表
    /// </summary>
    public List<Vector3> VertexList;

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, ObjPart> PartDict;

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string,Material> MatsDict;

    /// <summary>
    /// 对象名字
    /// </summary>
    public string Name = "";

    public ObjPart LastPart;

    /// <summary>
    /// 模型缩放值
    /// </summary>
    private Vector3 _scaleSize = Vector3.zero;

    public ObjModel(string name)
    {
        Name = name;
        UVList = new List<Vector2>();
        NormalList = new List<Vector3>();
        VertexList = new List<Vector3>();
        PartDict = new Dictionary<string, ObjPart>();
        MatsDict = new Dictionary<string, Material>();
    }

    public void AddObjPart(string name)
    {
        if (PartDict.ContainsKey(name))
            return;
 
        ObjPart part = new ObjPart(name);
        LastPart = part;
        PartDict.Add(name,part);
    }

    public Material GetMaterial(string name)
    {
        if (MatsDict.ContainsKey(name))
            return MatsDict[name];

        Material mat = new Material(Shader.Find("Standard"))
        {
            name = name
        };
        return mat;
    }

    public GameObject CreateGameObject()
    {
        if (PartDict.Count == 0)
            return null;

        GameObject obj = new GameObject(Name);
        obj.transform.localScale = Vector3.one;//GetScale();
        foreach(ObjPart part in PartDict.Values)
        {
            GameObject child = part.GetObj(this);
            child.transform.SetParent(obj.transform);
            child.transform.localScale -= new Vector3(child.transform.localScale.x * 2,0,0);
        }

        return obj;
    }

    public Vector3 GetScale()
    {
        if (_scaleSize != Vector3.zero)
            return _scaleSize;

        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        for (int i = 0; i < VertexList.Count; i++)
        {
            Vector3 point = VertexList[i];
            min.x = point.x < min.x ? point.x : min.x;
            min.y = point.y < min.y ? point.y : min.y;
            min.z = point.z < min.z ? point.z : min.z;
            max.x = point.x > max.x ? point.x : max.x;
            max.y = point.y > max.y ? point.y : max.y;
            max.z = point.z > max.z ? point.z : max.z;
        }

        _scaleSize = max - min;
        return _scaleSize;
    }
}