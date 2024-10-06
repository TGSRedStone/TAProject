using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SmoothNormals : MonoBehaviour
{
    [SerializeField] private Mesh[] meshs;

    private void Awake()
    {
        foreach (var mesh in meshs)
        {
            IEnumerable<IEnumerable<KeyValuePair<Vector3, int>>> groups = mesh.vertices
                .Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);
            
            Vector3[] normals = mesh.normals;
            Vector4[] smoothNormals = normals.Select((normal, index) => new Vector4(normal.x, normal.y, normal.z)).ToArray();
            
            foreach (var group in groups)
            {
                if (group.Count() == 1)
                {
                    continue;
                }
            
                Vector3 smoothNormal = Vector3.zero;
            
                foreach (var pair in group)
                {
                    smoothNormal += normals[pair.Value];
                }
                smoothNormal.Normalize();
            
                foreach (var pair in group)
                {
                    smoothNormals[pair.Value] = new Vector4(smoothNormal.x, smoothNormal.y, smoothNormal.z);
                }
            }

            // SmoothNormal(mesh);
            mesh.tangents = smoothNormals;
        }
    }
    
    private struct WeightedNormal
    {
        public Vector3 normal;
        public float weight;
    }
    
    private void SmoothNormal(Mesh mesh)
    {
        //创建一个字典，存顶点-顶点所有法线的键值对，顶点由唯一的Vector3来确定
        var normalDict = new Dictionary<Vector3, List<WeightedNormal>>();
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var tangents = mesh.tangents;
        var smoothNormals = mesh.normals;
        //三角形个数为triangles数组的长度/3
        var n = triangles.Length / 3;
        for (var i = 0; i < n; i++)
        {
            //第i个三角形的三个顶点的索引分别是i*3,i*3+1,i*3=2
            var vertexIndices = new[] {triangles[i * 3], triangles[i * 3 + 1], triangles[i * 3 + 2]};
            for (var j = 0; j < 3; j++)
            {
                var vertexIndex = vertexIndices[j];
                var vertexPosition = vertices[vertexIndex];
                if (!normalDict.ContainsKey(vertexPosition))
                {
                    normalDict.Add(vertexPosition, new List<WeightedNormal>());
                }

                WeightedNormal weightedNormal;
                weightedNormal.normal = normals[vertexIndex];
                //获取当前顶点出发的两条边
                var lineA = Vector3.zero;
                var lineB = Vector3.zero;
                if (j == 0)
                {
                    lineA = vertices[vertexIndices[1]] - vertexPosition;
                    lineB = vertices[vertexIndices[2]] - vertexPosition;
                }
                else if (j == 1)
                {
                    lineA = vertices[vertexIndices[0]] - vertexPosition;
                    lineB = vertices[vertexIndices[2]] - vertexPosition;
                }
                else if (j == 3)
                {
                    lineA = vertices[vertexIndices[0]] - vertexPosition;
                    lineB = vertices[vertexIndices[1]] - vertexPosition;
                }
                //把角度作为权重，记录起来，
                var angle = Vector3.Angle(lineA, lineB) * Mathf.Deg2Rad;
                weightedNormal.weight = angle;
                normalDict[vertexPosition].Add(weightedNormal);
            }
        }

        for (var index = 0; index < vertices.Length; index++)
        {
            var vertex = vertices[index];
            var weightedNormalList = normalDict[vertex];
            var smoothNormal = Vector3.zero;
            float weightSum = 0;
            foreach (var weightedNormal in weightedNormalList)
            {
                weightSum += weightedNormal.weight;
            }

            foreach (var weightedNormal in weightedNormalList)
            {
                smoothNormal = smoothNormal + weightedNormal.normal * weightedNormal.weight / weightSum;
            }

            smoothNormal = smoothNormal.normalized;
            smoothNormals[index] = smoothNormal;
            //构建三个正交向量，作为切线空间的坐标轴
            var normal = normals[index];
            var tangent = tangents[index];
            var biTangent = (Vector3.Cross(normal, tangent) * tangent.w).normalized;
            //构建TBN矩阵
            var tbn = new Matrix4x4(tangent, biTangent, normal, Vector3.zero);
            //相当于smoothNormal重新投影到三个坐标轴上，获取在切线空间下的法线。
            smoothNormals[index] = tbn.transpose.MultiplyVector(smoothNormal).normalized;
        }

        mesh.SetUVs(7, smoothNormals);
        Debug.Log("平滑法线成功");
    }
}
