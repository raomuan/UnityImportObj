using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Header("Assets的同级目录下 Model文件下的文件名")]
    public string ObjName;
	void Start ()
	{
	    string path = Application.dataPath + "/../Model/" + ObjName + ".obj";
        string objText = File.ReadAllText(path);
        //解析obj文件 obj文件其实就是一个文本文件
        ObjModel mode = ObjParse.ParseObj(objText);
        //加载材质球
        if(!string.IsNullOrEmpty(mode.MtlName))
        {
            string mtlPath = path.Substring(0, path.LastIndexOf('/') + 1) + mode.MtlName;
            mode.MatsDict = ObjParse.ParseMtl(File.ReadAllText(mtlPath));
        }

        mode.CreateGameObject();
	}
}
