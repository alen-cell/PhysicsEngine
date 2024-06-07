using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshToParticleColliders : MonoBehaviour
{

    public List<Vector3> Positions = new List<Vector3>();
    protected Vector3[] vertices;
    protected int[] indices;
    private Color[] colors;
    //public GameObject[] MeshToGenerateParticles;
    [Range(0, 1)]
    public float ScaleRender = 0.5f;
    public bool RescaleResolution;
    [Range(0, 10)]
    public int vertexskipSetup;
    public float VertexCount;
    public float triangleCount;
    public bool usingTriangle;
    public Transform Rigidtransform;
    public static HashSet<MeshToParticleColliders> Colliders = new HashSet<MeshToParticleColliders>();

    public Matrix4x4 TRS;
    public int NumMeshParticle = 0;
    // Start is called before the first frame update
    void Start()
    {
        //init
        var mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        Debug.Log(mesh.vertexCount);
        VertexCount = mesh.vertexCount;
        GameObject go = GetComponent<GameObject>();
        indices = mesh.GetIndices(0);
        float scaling = ScaleRender;
        TRS = this.transform.localToWorldMatrix;
/*
        if (usingTriangle == false)
        {
            NumMeshParticle = mesh.vertexCount;
            for (int i = 0; i < mesh.vertexCount - 1; i++)
            {
                // Debug.Log(vertices[i]);

                //  GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //  Debug.Log(indices[i]);

                //  particle.transform.parent = this.transform;
                // particle.transform.localPosition = vertices[i];
                //  particle.transform.localScale = new Vector3(scaling, scaling, scaling);

                Positions.Add(this.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[i]));
                //Positions.Add(particle.transform.position);

                //Debug.Log( Positions.Count);

            }
            Debug.Log("Added" + NumMeshParticle.ToString());

        }

        else
        {
            NumMeshParticle = mesh.triangles.Length;

            for (int i = 0; i < mesh.triangles.Length; i += 1)
            {

                Vector3 vertex1 = vertices[indices[i]];
                Vector3 vertex2 = vertices[indices[i + 1]];
                Vector3 vertex3 = vertices[indices[i + 2]];
                Vector3 TriangleCenter = (vertex1 + vertex2 + vertex3) / 3;
                GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Debug.Log(indices[i]);

                particle.transform.parent = this.transform;
                particle.transform.localPosition = TriangleCenter;
                particle.transform.localScale = new Vector3(scaling, scaling, scaling);

            }
        }
*/

    }
    // Update is called once per frame
    private void OnEnable()
    {
        Colliders.Add(this);
    }
}