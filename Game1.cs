//Name: Sohil Vaidya

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace Lab10_Mono
{

    #region Structures
    //vertex structure wll hold calculated normal data
    public struct VertexNormal
    {
        public Vector3 Normal;
        public readonly static VertexDeclaration vertexDeclaration = new
       VertexDeclaration
        (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Normal,
       0)
        );
    }
    struct Bullet
    {
        public Vector3 position;
        public Quaternion rotation;
    }
    public struct VertexExplosion : IVertexType
    {
        public Vector3 Position;
        public Vector4 TexCoord;
        public Vector4 AdditionalInfo;
        public readonly static VertexDeclaration vertexDeclaration = new
       VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position,
0),
 new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector4,
VertexElementUsage.TextureCoordinate, 0),
 new VertexElement(sizeof(float) * 7, VertexElementFormat.Vector4,
VertexElementUsage.TextureCoordinate, 1)
 );
        public VertexExplosion(Vector3 position, Vector4 texCoord, Vector4
       additionalInfo)
        {
            this.Position = position;
            this.TexCoord = texCoord;
            this.AdditionalInfo = additionalInfo;
        }
        // We have to implement the IVertexType interface so that draw will know about
        //our funky vertices.
         VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return
vertexDeclaration;
            }
        }
    }

    #endregion

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {

        #region Constants
        // Constants
        Vector3 lightDirection = new Vector3(3, -2, 5);
        public const float CITY_AMBIENT_LIGHT = 0.5f;
        // add a little extra to make the model stand out
        public const float MODEL_AMBIENT_LIGHT = 0.7f;

        public const int MAX_TARGETS = 50;

        public const int PARTICLES_PER_EXPLOSION = 80;
        #endregion

        #region Instance Variables
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GraphicsDevice device;
        Effect effect;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        Texture2D sceneryTexture;
        int[,] floorPlan;
        VertexBuffer cityVertexBuffer;
        Model xwingModel;
        int[] buildingHeights = new int[] { 0, 2, 2, 6, 5, 4 };
        VertexBuffer[][] modelNormalsVertexBuffer;
        VertexBuffer[][] targetNormalsVertexBuffer;
        Vector3 xwingPosition = new Vector3(8, 1, -3);
        Quaternion xwingRotation = Quaternion.Identity;
        float gameSpeed = 1.0f;
        enum CollisionType { None, Building, Boundary, Target }
        BoundingBox[] buildingBoundingBoxes;
        BoundingBox completeCityBox;
        Model targetModel;
        List<BoundingSphere> targetList = new List<BoundingSphere>();
        Texture2D bulletTexture;
        List<Bullet> bulletList = new List<Bullet>();
        double lastBulletTime = 0;
        Vector3 cameraPosition;
        Vector3 cameraUpDirection;
        Texture2D[] skyboxTextures;
        Model skyboxModel;
        Quaternion cameraRotation = Quaternion.Identity;
        Random random = new Random();
        VertexExplosion[] explosionVertices;
        GameTime gmtime;
        Texture2D explosionTexture;
        SoundEffect explosion;

        #endregion

        #region Constructor
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        #endregion

        #region Explosion Methods
        private void AddExplosion(Vector3 center, float time)
        {
            explosionVertices = new VertexExplosion[PARTICLES_PER_EXPLOSION * 6];
            int i = 0;
            for (int j = 0; j < PARTICLES_PER_EXPLOSION; j++)
            {
                // Give each particle a unique random direction in the range [-.5,+.5]
                //range
            float r1 = (float)random.NextDouble() - 0.5f;
                float r2 = (float)random.NextDouble() - 0.5f;
                float r3 = (float)random.NextDouble() - 0.5f;
                Vector3 moveDirection = new Vector3(r1, r2, r3);
                // Now make all random directions equal length
                moveDirection.Normalize();
                // Add some uniqueness to each particle (we will adjust the speed and
                //size using this value)
            float r4 = (float)random.NextDouble();
            // Normalize to [0.25,1.0] region (we don't want a particle speed of
            //zero)
            r4 = (r4 * 0.75f) + 0.25f;
            // Create six vertices for each particle (need two triangles for each
            //texture)
            explosionVertices[i++] = new VertexExplosion(center, new Vector4(1, 1,
            time, 1000), new Vector4(moveDirection, r4));
            explosionVertices[i++] = new VertexExplosion(center, new Vector4(0, 0,
           time, 1000), new Vector4(moveDirection, r4));
            explosionVertices[i++] = new VertexExplosion(center, new Vector4(1, 0,
           time, 1000), new Vector4(moveDirection, r4));
            explosionVertices[i++] = new VertexExplosion(center, new Vector4(1, 1,
           time, 1000), new Vector4(moveDirection, r4));
            explosionVertices[i++] = new VertexExplosion(center, new Vector4(0, 1,
           time, 1000), new Vector4(moveDirection, r4));
            explosionVertices[i++] = new VertexExplosion(center, new Vector4(0, 0,
           time, 1000), new Vector4(moveDirection, r4));
        }
    }
    private void DrawExplosion(float time)
    {
        if (explosionVertices != null)
        {
            device.BlendState = BlendState.Additive;
            device.DepthStencilState = DepthStencilState.DepthRead;
            effect.CurrentTechnique = effect.Techniques["Explosion"];
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xCamPos"].SetValue(cameraPosition);
            effect.Parameters["xExplosionTexture"].SetValue(explosionTexture);
            effect.Parameters["xCamUp"].SetValue(cameraUpDirection);
            effect.Parameters["xTime"].SetValue(time);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives<VertexExplosion>(PrimitiveType.TriangleList, explosionVertices,
                0, explosionVertices.Length / 3);
            }
            device.BlendState = BlendState.Opaque;
                device.DepthStencilState = DepthStencilState.Default;
            }
        }
        #endregion

        #region Collision Methods
        private void SetUpBoundingBoxes()
        {
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);
            List<BoundingBox> bbList = new List<BoundingBox>(); for (int x = 0; x <
           cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {
                    int buildingType = floorPlan[x, z];
                    if (buildingType != 0)
                    {
                        int buildingHeight = buildingHeights[buildingType];
                        Vector3[] buildingPoints = new Vector3[2];
                        buildingPoints[0] = new Vector3(x, 0, -z);
                        buildingPoints[1] = new Vector3(x + 1, buildingHeight, -z - 1);
                        BoundingBox buildingBox =
                       BoundingBox.CreateFromPoints(buildingPoints);
                        bbList.Add(buildingBox);
                    }
                }
            }
            buildingBoundingBoxes = bbList.ToArray();
            Vector3[] boundaryPoints = new Vector3[2];
            boundaryPoints[0] = new Vector3(0, 0, 0);
            boundaryPoints[1] = new Vector3(cityWidth, 20, -cityLength);
            completeCityBox = BoundingBox.CreateFromPoints(boundaryPoints);
        }
        private CollisionType CheckCollision(BoundingSphere sphere)
        {
            for (int i = 0; i < buildingBoundingBoxes.Length; i++)
                if (buildingBoundingBoxes[i].Contains(sphere) !=
               ContainmentType.Disjoint)
                    return CollisionType.Building;
            if (completeCityBox.Contains(sphere) != ContainmentType.Contains)
                return CollisionType.Boundary;
            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i].Contains(sphere) != ContainmentType.Disjoint)
                {
                    targetList.RemoveAt(i);
                    i--;
                    return CollisionType.Target;
                }
            }
            return CollisionType.None;
        }
        #endregion

        #region Camera Methods
        private void SetUpCamera()
        {
            viewMatrix = Matrix.CreateLookAt(new Vector3(20, 13, -5), new Vector3(8, 0, -7), new Vector3(0, 1, 0));
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.2f, 500.0f);

            //OPTIONAL TEST : DIFFERENT CAMERA POSITION TO VIEW THE LIGHTS MORE EFFECTIVELY
            //viewMatrix = Matrix.CreateLookAt(new Vector3(25, 20, -20), new Vector3(8, 0, -7), new Vector3(0, 1, 0));
            //projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.2f, 500.0f);

        }

        private void UpdateCamera()
        {
            cameraRotation = Quaternion.Lerp(cameraRotation, xwingRotation, 0.1f);
            Vector3 campos = new Vector3(0, 0.1f, 0.6f);
            campos = Vector3.Transform(campos,
           Matrix.CreateFromQuaternion(cameraRotation));
            campos += xwingPosition;
            Vector3 camup = new Vector3(0, 1, 0);
            camup = Vector3.Transform(camup,
           Matrix.CreateFromQuaternion(cameraRotation));
            viewMatrix = Matrix.CreateLookAt(campos, xwingPosition, camup);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
           device.Viewport.AspectRatio, 0.2f, 500.0f);
            cameraPosition = campos;
            cameraUpDirection = camup;
        }
        #endregion

        #region IO Methods
        private void ProcessKeyboard(GameTime gameTime)
        {
            float leftRightRot = 0;
            float turningSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds /
           1000.0f;
            turningSpeed *= 1.6f * gameSpeed;
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Right))
                leftRightRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Left))
                leftRightRot -= turningSpeed;
            float upDownRot = 0;
            if (keys.IsKeyDown(Keys.Down))
                upDownRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Up))
                upDownRot -= turningSpeed;
            if (keys.IsKeyDown(Keys.Space))
            {
                double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
                if (currentTime - lastBulletTime > 100)
                {
                    Bullet newBullet = new Bullet();
                    newBullet.position = xwingPosition;
                    newBullet.rotation = xwingRotation;
                    bulletList.Add(newBullet);
                    lastBulletTime = currentTime;
                }
            }
            Quaternion additionalRot = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -
           1),
            leftRightRot) * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0),
           upDownRot);
            xwingRotation *= additionalRot;
        }





        #endregion
        #region Model Methods
        private Model LoadModel(string assetName, ref VertexBuffer[][]
       normalsVertexBuffer)
        {
            Model newModel = Content.Load<Model>(assetName);
            // A two-dimensional array of vertex buffers, one for each part in each mesh
            normalsVertexBuffer = new VertexBuffer[newModel.Meshes.Count][];
            int meshNum = 0; // to keep track of which mesh we are working with
            foreach (ModelMesh mesh in newModel.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone();
                if (meshNum < newModel.Meshes.Count)
                {
                    CalculateNormals(mesh, meshNum, ref normalsVertexBuffer);
                    meshNum++;
                }
            }
            return newModel;
        }
        private void CalculateNormals(ModelMesh mesh, int meshNum, ref VertexBuffer[][]
        normalsVertexBuffer)
        {
            // meshNum is the index of the current mesh
            normalsVertexBuffer[meshNum] = new VertexBuffer[mesh.MeshParts.Count];
            for (int partNum = 0; partNum < mesh.MeshParts.Count; partNum++) // for all
                //parts in this mesh
         {
                var part = mesh.MeshParts[partNum]; // get a part
                                                    // the array of vertex normals for this part
                VertexNormal[] normalVertices = new
               VertexNormal[part.VertexBuffer.VertexCount];
                // the vertex buffer for this part, of which we have one for each part in
                //each mesh
 normalsVertexBuffer[meshNum][partNum] = new VertexBuffer(device,
VertexNormal.vertexDeclaration,
 part.VertexBuffer.VertexCount, BufferUsage.WriteOnly);
                int numIndices = part.IndexBuffer.IndexCount;
                // extract a copy of the vertex and index buffers from the part
                VertexPositionColor[] partVertices = new
               VertexPositionColor[part.VertexBuffer.VertexCount];
                part.VertexBuffer.GetData<VertexPositionColor>(partVertices);
                ushort[] partIndices = new ushort[part.IndexBuffer.IndexCount];
                part.IndexBuffer.GetData<ushort>(partIndices);
                // Initialize all normal data to zero
                for (int j = 0; j < part.VertexBuffer.VertexCount; j++)
                    normalVertices[j].Normal = new Vector3(0, 0, 0);
                // Compute the face normals. There is one of these per triangle, where
                //each
 // triangle is defined by three consecutive indices in the index buffer
 int numTriangles = numIndices / 3;
                Vector3[] faceNormals = new Vector3[numTriangles];
                int i = 0;
                for (int indexNum = 0; indexNum < numIndices; indexNum += 3)
                {
                    // get the three indices of this triangle
                    int index1 = partIndices[indexNum];
                    int index2 = partIndices[indexNum + 1];
                    int index3 = partIndices[indexNum + 2];
                    // Compute two side vectors using the vertex pointed to by index1 as
                    //the origin
                // Make sure we put them in the right order (using right-hand rule),
                // so backface culling doesn't mess things up.
                    Vector3 side1 = partVertices[index3].Position -
partVertices[index1].Position;
                    Vector3 side2 = partVertices[index2].Position -
                   partVertices[index1].Position;
                    // The normal is the cross product of the two sides that meet at the
                    //vertex
                     faceNormals[i] = Vector3.Cross(side1, side2);
                    i++;
                }
                // Build the adjacent triangle list
                // For each vertex, look at the constituent vertices of all triangles.
                // If a triangle uses this vertex, add that triangle number to the list
                // of adjacent triangles; making sure that we only add it once
                // There is an adjacent triangle list for each vertex but the numbers
                //stored
                // in the list are triangle numbers (defined by each triplet of indices
                //in the
                // index buffer
                List<int>[] adjacentTriangles = new
               List<int>[part.VertexBuffer.VertexCount];
                int thisTriangle = 0;
                for (int j = 0; j < part.VertexBuffer.VertexCount; j++)
                {
                    adjacentTriangles[j] = new List<int>();
                    for (int k = 0; k < numIndices; k += 3)
                    {
                        thisTriangle = k / 3;
                        if (adjacentTriangles[j].Contains(thisTriangle)) continue;
                        else if ((partIndices[k] == j) || (partIndices[k + 1] == j) ||
                       (partIndices[k + 2] == j))
                        {
                            adjacentTriangles[j].Add(thisTriangle);
                        }
                    }
                }
                // We now have face normals and adjacent triangles for all vertices.
                // Since we computed the face normals using cross product, the
                // magnitude of the face normals is proportional to the area of the
                //triangle.
 // So, all we need to do is sum the face normals of all adjacent
//triangles
 // to get the vertex normal, and then normalize.
 Vector3 sum = new Vector3(0, 0, 0);
                // For all vertices in this part
                for (int v = 0; v < part.VertexBuffer.VertexCount; v++)
                {
                    sum = Vector3.Zero;
                    foreach (int idx in adjacentTriangles[v]) // for all adjacent
                        //triangles of this vertex
                {
                        // The indices stored in the adjacent triangles list are triangle
                        //numbers,
 // which, conveniently, is how we indexed the face normal array.
// Thus, we are computing a sum weighted by triangle area.
sum += faceNormals[idx];
                    }
                    // Alternative: average the face normals (Gourard Shading)
                    // Do this only if Gourard Shading
                    sum /= adjacentTriangles[v].Count;
                    //// Gourard
                    if (sum != Vector3.Zero)
                    {
                        sum.Normalize();
                    }
                    normalVertices[v].Normal = sum;
                }
                // Copy the normal information for this part into the appropriate vertex
                //buffer
                 normalsVertexBuffer[meshNum][partNum].SetData(normalVertices);
            }
        }
        private void DrawModel()
        {
            Matrix worldMatrix = Matrix.CreateScale(0.0005f, 0.0005f, 0.0005f)
            * Matrix.CreateRotationY(MathHelper.Pi) *
           Matrix.CreateFromQuaternion(xwingRotation)
            * Matrix.CreateTranslation(xwingPosition);
            Matrix[] xwingTransforms = new Matrix[xwingModel.Bones.Count];
            xwingModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            int meshIndex = 0; // to keep track of which mesh we are drawing
            foreach (ModelMesh mesh in xwingModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique =
                   currentEffect.Techniques["ColoredNormal"];

                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] *
                    worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(MODEL_AMBIENT_LIGHT);
                }
                DrawMesh(mesh, meshIndex, ref modelNormalsVertexBuffer);
                meshIndex++;
            }
        }
        public void DrawMesh(ModelMesh mesh, int meshIndex, ref VertexBuffer[][]
        normalsVertexBuffer)
        // Adapted from from Monogame source (ModelMesh.Draw), Version 3.7
        // We did this to get direct access to the vertex and index buffers
        {
            for (int i = 0; i < mesh.MeshParts.Count; i++)
            {
                var part = mesh.MeshParts[i];
                var effect = part.Effect;
                // SetVertexBuffers requires that we use VertexBufferBindings
                // Either of the constructs work to bind our two vertex buffers
                //VertexBufferBinding[] bindings = new VertexBufferBinding[2];
                //bindings[0] = new VertexBufferBinding(part.VertexBuffer, 0, 0);
                //bindings[1] = new
                //VertexBufferBinding(normalsVertexBuffer[meshIndex][i], 0, 0);
                VertexBufferBinding vbb1 = new VertexBufferBinding(part.VertexBuffer, 0);
                VertexBufferBinding vbb2 = new
               VertexBufferBinding(normalsVertexBuffer[meshIndex][i], 0);
                if (part.PrimitiveCount > 0)
                {
                    //device.SetVertexBuffers(bindings);
                    device.SetVertexBuffers(vbb1, vbb2);
                    device.Indices = part.IndexBuffer;
                    for (int j = 0; j < effect.CurrentTechnique.Passes.Count; j++)
                    {
                        effect.CurrentTechnique.Passes[j].Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                        part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
        }
        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float
       speed)
        {
            Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -1), rotationQuat);
            position += addVector * speed;
        }
        #endregion

        #region City Methods
        private void DrawCity()
        {
            effect.CurrentTechnique = effect.Techniques["Textured"];
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xTexture"].SetValue(sceneryTexture);
            effect.Parameters["xEnableLighting"].SetValue(true);
            effect.Parameters["xLightDirection"].SetValue(lightDirection);
            effect.Parameters["xAmbient"].SetValue(CITY_AMBIENT_LIGHT);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.SetVertexBuffer(cityVertexBuffer);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0,
               cityVertexBuffer.VertexCount / 3);
            }
        }
        private void LoadFloorPlan()
        {
            floorPlan = new int[,]
            {
 {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
 {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,0,1,1,0,0,0,1,1,0,0,1,0,1},
 {1,0,0,1,1,0,0,0,1,0,0,0,1,0,1},
 {1,0,0,0,1,1,0,1,1,0,0,0,0,0,1},
 {1,0,0,0,0,0,0,0,0,0,0,1,0,0,1},
 {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,1,1,0,0,0,1,0,0,0,0,0,0,1},
 {1,0,1,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
 {1,0,0,0,0,1,0,0,0,0,0,0,0,0,1},
 {1,0,0,0,0,1,0,0,0,1,0,0,0,0,1},
 {1,0,1,0,0,0,0,0,0,1,0,0,0,0,1},
 {1,0,1,1,0,0,0,0,1,1,0,0,0,1,1},
 {1,0,0,0,0,0,0,0,1,1,0,0,0,1,1},
 {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
 };
            Random random = new Random();
            int differentBuildings = buildingHeights.Length - 1;
            for (int x = 0; x < floorPlan.GetLength(0); x++)
                for (int y = 0; y < floorPlan.GetLength(1); y++)
                    if (floorPlan[x, y] == 1)
                        floorPlan[x, y] = random.Next(differentBuildings) + 1;
        }
        private void SetUpVertices()
        {
            int differentBuildings = buildingHeights.Length - 1;
            float imagesInTexture = 1 + differentBuildings * 2;
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);
            List<VertexPositionNormalTexture> verticesList = new List<VertexPositionNormalTexture>();
            for (int x = 0; x < cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {
                    int currentbuilding = floorPlan[x, z];
                    //floor or ceiling
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                    buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2(currentbuilding
                    * 2 / imagesInTexture, 1)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                   buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new
                   Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1,
                   buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2((currentbuilding
                   * 2 + 1) / imagesInTexture, 1)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                   buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new
                   Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1,
                   buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new
                   Vector2((currentbuilding * 2 + 1) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1,
                   buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2((currentbuilding
                   * 2 + 1) / imagesInTexture, 1)));
                    if (currentbuilding != 0)
                    {
                        //front wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                        1, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) /
imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new
                       Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) /
                       imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new
                       Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) /
                       imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new
                       Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        //back wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                        1, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture,
                        1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture,
                       1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding
                       * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding
                       * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new
                       Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture,
                       1)));
                        //left wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       0, -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       0, -z - 1), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) /
                       imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       buildingHeights[currentbuilding], -z - 1), new Vector3(-1, 0, 0), new
                       Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       buildingHeights[currentbuilding], -z - 1), new Vector3(-1, 0, 0), new
                       Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       buildingHeights[currentbuilding], -z), new Vector3(-1, 0, 0), new
                       Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x,
                       0, -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        //right wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                        1, 0, -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture,
                        1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, buildingHeights[currentbuilding], -z - 1), new Vector3(1, 0, 0), new
                       Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, 0, -z - 1), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) /
                       imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, buildingHeights[currentbuilding], -z - 1), new Vector3(1, 0, 0), new
                       Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, 0, -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture,
                       1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x +
                       1, buildingHeights[currentbuilding], -z), new Vector3(1, 0, 0), new
                       Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    }
                }
            }
            cityVertexBuffer = new VertexBuffer(device,
            VertexPositionNormalTexture.VertexDeclaration, verticesList.Count,
           BufferUsage.WriteOnly);

            cityVertexBuffer.SetData<VertexPositionNormalTexture>(verticesList.ToArray());
        }


        #endregion

        #region Target Methods
        private void AddTargets()
        {
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);
            while (targetList.Count < MAX_TARGETS)
            {
                int x = random.Next(cityWidth);
                int z = -random.Next(cityLength);
                float y = (float)random.Next(2000) / 1000f + 1;
                float radius = (float)random.Next(1000) / 1000f * 0.2f + 0.01f;
                BoundingSphere newTarget = new BoundingSphere(new Vector3(x, y, z),
               radius);
                if (CheckCollision(newTarget) == CollisionType.None)
                    targetList.Add(newTarget);
            }
        }
        private void DrawTargets()
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                Matrix worldMatrix = Matrix.CreateScale(targetList[i].Radius) *
               Matrix.CreateTranslation(targetList[i].Center);
                Matrix[] targetTransforms = new Matrix[targetModel.Bones.Count];
                targetModel.CopyAbsoluteBoneTransformsTo(targetTransforms);
                int meshIndex = 0; // to keep track of which mesh we are drawing
                foreach (ModelMesh modmesh in targetModel.Meshes)
                {
                    foreach (Effect currentEffect in modmesh.Effects)
                    {
                        currentEffect.CurrentTechnique =
                       currentEffect.Techniques["ColoredNormal"];
                        currentEffect.Parameters["xWorld"].SetValue
                        (targetTransforms[modmesh.ParentBone.Index] * worldMatrix);
                        currentEffect.Parameters["xView"].SetValue(viewMatrix);
                        currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                        currentEffect.Parameters["xEnableLighting"].SetValue(true);

                        currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                        currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    }
                    DrawMesh(modmesh, meshIndex, ref targetNormalsVertexBuffer);
                    meshIndex++;
                }
            }
        }
        #endregion

        #region Bullet Methods
        private void UpdateBulletPositions(float moveSpeed)
        {
            for (int i = 0; i < bulletList.Count; i++)
            {
                Bullet currentBullet = bulletList[i];
                MoveForward(ref currentBullet.position, currentBullet.rotation, moveSpeed
               * 2.0f);
                bulletList[i] = currentBullet;
                BoundingSphere bulletSphere = new BoundingSphere(currentBullet.position,
               0.05f);
                CollisionType colType = CheckCollision(bulletSphere);
                if (colType == CollisionType.Target)
                {
                    //Create an explosion at this position
                    AddExplosion(currentBullet.position,
                    (float)gmtime.TotalGameTime.TotalMilliseconds);
                    explosion.Play();
                    //And speed things up a bit
                    gameSpeed *= 1.05f;
                }
                if (colType != CollisionType.None)
                {
                    //If we collided, delete the bullet
                    bulletList.RemoveAt(i);
                    i--;
                }
            }
        }
        private void DrawBullets()
        {
            if (bulletList.Count > 0)
            {
                VertexPositionTexture[] bulletVertices = new
                VertexPositionTexture[bulletList.Count * 6];
                int i = 0;
                foreach (Bullet currentBullet in bulletList)
                {
                    Vector3 center = currentBullet.position;
                    bulletVertices[i++] = new VertexPositionTexture(center, new
                   Vector2(1, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new
                   Vector2(0, 0));
                    bulletVertices[i++] = new VertexPositionTexture(center, new
                   Vector2(1, 0));
                    bulletVertices[i++] = new VertexPositionTexture(center, new
                   Vector2(1, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new
                   Vector2(0, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new
                   Vector2(0, 0));
                }
                effect.CurrentTechnique = effect.Techniques["PointSprites"];
                effect.Parameters["xWorld"].SetValue(Matrix.Identity);
                effect.Parameters["xProjection"].SetValue(projectionMatrix);
                effect.Parameters["xView"].SetValue(viewMatrix);
                effect.Parameters["xCamPos"].SetValue(cameraPosition);
                effect.Parameters["xTexture"].SetValue(bulletTexture);
                effect.Parameters["xCamUp"].SetValue(cameraUpDirection);
                effect.Parameters["xPointSpriteSize"].SetValue(0.1f);
                device.BlendState = BlendState.Additive;
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleList, bulletVertices,
                   0, bulletList.Count * 2);
                }
            }
            device.BlendState = BlendState.Opaque;
        }
        #endregion

        #region SkyBox Methods
        private Model LoadModel(string assetName, out Texture2D[] textures)
        {
            Model newModel = Content.Load<Model>(assetName);
            textures = new Texture2D[newModel.Meshes[0].MeshParts.Count]; // only one
            //mesh
 int i = 0;
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (BasicEffect currentEffect in mesh.Effects)
                    textures[i++] = currentEffect.Texture;
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone();
            return newModel;
        }
        private void DrawSkybox()
        {
            SamplerState ss = new SamplerState();
            ss.AddressU = TextureAddressMode.Clamp;
            ss.AddressV = TextureAddressMode.Clamp;
            device.SamplerStates[0] = ss;
            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = false;
            device.DepthStencilState = dss;
            Matrix[] skyboxTransforms = new Matrix[skyboxModel.Bones.Count];
            skyboxModel.CopyAbsoluteBoneTransformsTo(skyboxTransforms);
            int i = 0;
            foreach (ModelMesh mesh in skyboxModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = skyboxTransforms[mesh.ParentBone.Index] *
                   Matrix.CreateTranslation(xwingPosition);
                    currentEffect.CurrentTechnique =
                   currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(skyboxTextures[i++]);
                }
                mesh.Draw();
            }
            dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            device.DepthStencilState = dss;
        }
        #endregion

        #region Game Methods
        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            graphics.PreferredBackBufferWidth = 900;
            graphics.PreferredBackBufferHeight = 900;
            graphics.ApplyChanges();
            Window.Title = "Lab10_Mono - FlightSim";

            LoadFloorPlan();

            lightDirection.Normalize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            device = graphics.GraphicsDevice;
            effect = Content.Load<Effect>("effects");
            sceneryTexture = Content.Load<Texture2D>("texturemap");
            xwingModel = LoadModel("xwing", ref modelNormalsVertexBuffer);
            targetModel = LoadModel("target", ref targetNormalsVertexBuffer);
            bulletTexture = Content.Load<Texture2D>("bullet");
            skyboxModel = LoadModel("skybox", out skyboxTextures);
            // Load the explosion resources
            explosion = Content.Load<SoundEffect>("explosion");
            explosionTexture = Content.Load<Texture2D>("explosiontexture");
            SetUpCamera();
            SetUpVertices();
            SetUpBoundingBoxes();
            AddTargets();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            ProcessKeyboard(gameTime);
            float moveSpeed = gameTime.ElapsedGameTime.Milliseconds / 500.0f * gameSpeed;
            MoveForward(ref xwingPosition, xwingRotation, moveSpeed);
            UpdateCamera();
            BoundingSphere xwingSphere = new BoundingSphere(xwingPosition, 0.04f);
            CollisionType hitSomething = CheckCollision(xwingSphere);
            if ((hitSomething == CollisionType.Boundary) || (hitSomething ==
           CollisionType.Building))
            {
                // If we hit the ground (or other boundary), or a building, blow up and
                //restart
            // We ignore target collisions for now
            //Create an explosion at this position
                AddExplosion(xwingPosition,
(float)gameTime.TotalGameTime.TotalMilliseconds);
                explosion.Play();
                // And restart
                xwingPosition = new Vector3(8, 1, -3);
                xwingRotation = Quaternion.Identity;
                gameSpeed /= 1.1f;
            }
            UpdateBulletPositions(moveSpeed);
            gmtime = gameTime;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkSlateBlue, 1.0f, 0);
            DrawSkybox();
            DrawCity();
            DrawModel();
            DrawTargets();
            DrawBullets();
            DrawExplosion((float)gameTime.TotalGameTime.TotalMilliseconds);

            base.Draw(gameTime);
        }
        #endregion
    }
}
