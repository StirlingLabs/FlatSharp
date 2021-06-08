﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using FlatSharp;

namespace BenchmarkCore.Modified
{
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [ShortRunJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp50, BenchmarkDotNet.Environments.Jit.RyuJit, BenchmarkDotNet.Environments.Platform.AnyCpu)]
    public class MeshServerModifiedUseCases
    {
        [Params(40)]
        public byte RegionSize = 40;

        [Params(0.40f)]
        public float MeshFillPercent = 0.4f;

        [Params(10)] public byte ClientsInChangeRadius = 10;

        public byte[] FakeDiskStoredMesh;
        public byte[] FakeDiskStoredRegion;
        private int fillSize;
        private int regionSizeCubed;
        private int regionSizeSquared;
        private Random rnd;

        private ISerializer<VoxelRegion3D> voxelSerializer;
        private ISerializer<Mesh> meshSerializer;

        // we aren't trying to benchmark the allocator and GC here. Assume an already-allocated buffer.
        private byte[] networkBuffer = new byte[100 * 1024 * 1024];

        [GlobalSetup]
        public void Setup()
        {
            this.meshSerializer = Mesh.Serializer;
            this.voxelSerializer = VoxelRegion3D.Serializer;

            regionSizeCubed = RegionSize * RegionSize * RegionSize;
            regionSizeSquared = RegionSize * RegionSize;

            fillSize = (int)(MeshFillPercent * regionSizeCubed);

            var region = new VoxelRegion3D();
            region.size = RegionSize;
            region.voxels = new List<Voxel>(regionSizeCubed);
            region.iteration = new MutableUInt32 { Value = 0 };
            region.location = new Vector3Int();
            rnd = new Random(0);
            for (int i = 0; i < regionSizeCubed; i++)
            {
                region.voxels.Add(new Voxel
                {
                    VoxelType = (byte)Math.Clamp(rnd.Next(-255, 255), 0, 255),
                    SubType = (byte)rnd.Next(255),
                    Hp = (byte)rnd.Next(255),
                    Unused = (byte)rnd.Next(255)
                });
            }

            SaveToDisk(region);

            // Update fillSize to accomodate max. Some items may be null.
            Mesh mesh = new Mesh
            {
                color = new List<Color>(),
                normals = new List<Vector3>(),
                triangles = new List<MutableUShort>(),
                uv = new List<Vector2>(),
                vertices = new List<Vector3>(),
                filledLength = new MutableUInt32 { Value = 0 }
            };

            for (int i = 0; i < fillSize * 3; ++i)
            {
                mesh.color.Add(new());
                mesh.normals.Add(new());
                mesh.triangles.Add(new());
                mesh.uv.Add(new());
                mesh.vertices.Add(new());
            }

            UpdateMeshInPlace(rnd, region, mesh);
            SaveToDisk(mesh);
        }

        private Mesh UpdateMeshInPlace(Random rnd, VoxelRegion3D voxelRegion3D, Mesh mesh)
        {
            int filled = 0;

            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var colors = mesh.color;
            var uvs = mesh.uv;
            var triangles = mesh.triangles;
            var voxels = voxelRegion3D.voxels;

            Voxel[] adjacentVoxels = new Voxel[8];
            for (int i = 0; i < regionSizeCubed - (regionSizeSquared + RegionSize + 1); i++)
            {
                adjacentVoxels[0] = voxels[i];
                adjacentVoxels[1] = voxels[i + 1];
                adjacentVoxels[2] = voxels[i + RegionSize];
                adjacentVoxels[3] = voxels[i + RegionSize + 1];
                adjacentVoxels[4] = voxels[regionSizeSquared + i];
                adjacentVoxels[5] = voxels[regionSizeSquared + i + 1];
                adjacentVoxels[6] = voxels[regionSizeSquared + i + RegionSize];
                adjacentVoxels[7] = voxels[regionSizeSquared + i + RegionSize + 1];

                byte meshMask = 0;
                for (int j = 0; j < adjacentVoxels.Length; j++)
                {
                    meshMask |= (byte)(Math.Min(adjacentVoxels[j].VoxelType, (byte)1) << j);
                }

                if (meshMask is > 0 and < 255 && filled < fillSize)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var vertex = vertices[filled];
                        vertex.x = rnd.Next();
                        vertex.y = rnd.Next();
                        vertex.z = rnd.Next();

                        var normal = vertices[filled];
                        normal.x = rnd.Next();
                        normal.y = rnd.Next();
                        normal.z = rnd.Next();

                        var color = colors[filled];
                        color.r = adjacentVoxels[0].VoxelType;
                        color.g = adjacentVoxels[0].SubType;
                        color.b = adjacentVoxels[0].Hp;
                        color.a = adjacentVoxels[0].Unused;

                        var uv = uvs[filled];
                        uv.x = 0f;
                        uv.y = 1f;

                        triangles[filled].Value = (ushort)filled;
                        filled++;
                    }
                }
            }

            return mesh;
        }

        private void FakeGrpcStreamRegionToClient(VoxelRegion3D region)
        {
            if (region is IFlatBufferDeserializedObject deserialized)
            {
                StreamDeserializedObjectToClient(deserialized);
            }
            else
            {
                var maxSize = this.voxelSerializer.GetMaxSize(region);
                this.voxelSerializer.Write(networkBuffer, region);
            }
        }

        private void FakeGrpcStreamMeshToClient(Mesh region)
        {
            if (region is IFlatBufferDeserializedObject deserialized)
            {
                StreamDeserializedObjectToClient(deserialized);
            }
            else
            {
                var maxSize = this.voxelSerializer.GetMaxSize(region);
                this.voxelSerializer.Write(networkBuffer, region);
            }
        }

        private void StreamDeserializedObjectToClient(IFlatBufferDeserializedObject region)
        {
            var length = region.InputBuffer.Length;
            region.InputBuffer.GetByteMemory(0, length).CopyTo(networkBuffer);
        }

        [Benchmark]
        public void SendRegionToClient()
        {
            var regionFromStorage = FakeDiskStoredRegion;
            var deserializedRegion = this.voxelSerializer.Parse(regionFromStorage);
            FakeGrpcStreamRegionToClient(deserializedRegion);
        }

        [Benchmark]
        public void SendVisibleRegionsToClient()
        {
            // Assume visible Regions to be 20 in each direction
            for (int x = 0; x < 20; x++)
            {
                for (int z = 0; z < 20; z++)
                {
                    var regionFromStorage = FakeDiskStoredRegion;
                    var deserializedRegion = this.voxelSerializer.Parse(regionFromStorage);
                    FakeGrpcStreamRegionToClient(deserializedRegion);
                }
            }
        }

        [Benchmark]
        public void SendVisibleMeshesToClient()
        {
            // Assume visible meshes to be 20 in each direction
            for (int x = 0; x < 20; x++)
            {
                for (int z = 0; z < 20; z++)
                {
                    var regionFromStorage = FakeDiskStoredRegion;
                    var deserializedRegion = this.voxelSerializer.Parse(regionFromStorage);
                    FakeGrpcStreamRegionToClient(deserializedRegion);
                }
            }
        }

        [Benchmark]
        public void SendMeshToClient()
        {
            var meshFromStorage = FakeDiskStoredMesh;
            var deserializedMesh = this.meshSerializer.Parse<Mesh>(meshFromStorage);
            FakeGrpcStreamMeshToClient(deserializedMesh);
        }

        [Benchmark]
        public void ModifyMeshAndSendToClients()
        {
            var regionFromStorage = FakeDiskStoredRegion;
            var deserializedRegion = this.voxelSerializer.Parse(regionFromStorage);

            Mesh mesh = this.meshSerializer.Parse<Mesh>(FakeDiskStoredMesh);
            var voxels = deserializedRegion.voxels;

            // simulate region data change
            voxels[rnd.Next(0, voxels.Count - 1)].VoxelType = (byte)rnd.Next(1);
            deserializedRegion.iteration.Value++;

            // generate new mesh
            UpdateMeshInPlace(rnd, deserializedRegion, mesh);

            // Not needed -- assuming we are using mmapped input.
            // Otherwise, we just need to write the full buffer back, but there is no serialization overhead there -- just file I/O.
            // SaveToDisk(mesh); 
            // SaveToDisk(deserializedRegion);

            for (int i = 0; i < ClientsInChangeRadius; i++)
            {
                // Not really needed. We can just memcpy the modified buffer.
                FakeGrpcStreamMeshToClient(mesh);
            }
        }

        private void SaveToDisk(VoxelRegion3D deserializedRegion)
        {
            FakeDiskStoredRegion = new byte[this.voxelSerializer.GetMaxSize(deserializedRegion)];
            int bytesWritten = this.voxelSerializer.Write(FakeDiskStoredRegion, deserializedRegion);

            FakeDiskStoredRegion = FakeDiskStoredRegion.AsSpan().Slice(0, bytesWritten).ToArray();
        }

        private void SaveToDisk(Mesh mesh)
        {
            FakeDiskStoredMesh = new byte[this.meshSerializer.GetMaxSize(mesh)];
            int bytesWritten = this.meshSerializer.Write(FakeDiskStoredMesh, mesh);

            FakeDiskStoredMesh = FakeDiskStoredMesh.AsSpan().Slice(0, bytesWritten).ToArray();
        }
    }
}