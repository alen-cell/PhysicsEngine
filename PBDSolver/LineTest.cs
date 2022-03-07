using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineTest : MonoBehaviour
{

    private int lineCount = 100;
    //每条线的长度
    private float radius = 3.0f;
    //划线使用的材质球
    static Material lineMaterial;
    /// <summary>
    /// 创建一个材质球
    /// </summary>
    static void CreateLineMaterial()
    {
        //如果材质球不存在
        if (!lineMaterial)
        {
            //用代码的方式实例一个材质球
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            //设置参数
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            //设置参数
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            //设置参数
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    /// <summary>
    /// 使用GL画线的回调
    /// </summary>
    public void OnRenderObject()
    {
        //创建材质球
        CreateLineMaterial();
        //激活第一个着色器通过（在本例中，我们知道它是唯一的通过）
        lineMaterial.SetPass(0);
        //渲染入栈  在Push——Pop之间写GL代码
        GL.PushMatrix();
        GL.LoadIdentity();

        GL.modelview = Camera.main.worldToCameraMatrix * Matrix4x4.identity;

        //GL.LoadProjectionMatrix(Camera.main.projectionMatrix);

        //矩阵相乘，将物体坐标转化为世界坐标
        GL.MultMatrix(transform.localToWorldMatrix);

        // 开始画线  在Begin——End之间写画线方式
        //GL.LINES 画线
        GL.Begin(GL.LINES);
        for (int i = 0; i < lineCount; ++i)
        {
            float a = i / (float)lineCount;
            float angle = a * Mathf.PI * 2;
            // 设置颜色
            GL.Color(new Color(a, 1 - a, 0, 0.8F));
            //画线起始点
            GL.Vertex3(0, 0, 0);
            // 划线重点
            GL.Vertex3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
        GL.End();
        //渲染出栈
        GL.PopMatrix();
    }

    void Update()
    {

    }
}
