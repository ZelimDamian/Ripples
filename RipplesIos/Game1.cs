using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace RippleIos
{
	public struct VertexPositionNormalTextureTangentBinormal
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TextureCoordinate;
		public Vector3 Tangent;
		public Vector3 Binormal;
		
		public static readonly VertexElement[] VertexElements =
		new VertexElement[]
		{
			new VertexElement(sizeof(float) * 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
			new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
			new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
			new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
			new VertexElement(sizeof(float) * 11, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0),
		};
		
		public VertexPositionNormalTextureTangentBinormal(Vector3 position, Vector3 normal, Vector2 textureCoordinate, Vector3 tangent, Vector3 binormal)
		{
			Position = position;
			Normal = normal;
			TextureCoordinate = textureCoordinate;
			Tangent = tangent;
			Binormal = binormal;
		}
		
		public static int SizeInBytes { get { return sizeof(float) * 14; } }
	}
	
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		// Variables for Matrix calculations, viewport and object movment
		float width, height;
		float x = 0, y = 0;
		float zHeight = 15.0f;
		float moveObject = 0;
		
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		
		// 3D Object
		Model m_Model;
		Model m_ModelA;
		
		// Our effect object, this is where our shader will be loaded abd compiled
		Effect effect;
		Effect effectBlur;
		Effect effectBloom;
		Effect effectCombine;
		
		// Render target
		RenderTarget2D renderTarget;
		RenderTarget2D renderTargetBloom;
		RenderTarget2D renderTargetBlurBloom;
		RenderTarget2D renderTargetBlurIIBloom;
		Texture2D SceneTexture;
		Texture2D BloomTexture;
		Texture2D BlurBloomTexture;
		Texture2D BlurIIBloomTexture;
		
		// Textures
		Texture2D colorMap;
		Texture2D normalMap;
		Texture2D glossMap;
		Texture2D alphaMap;
		
		// Matrices
		Matrix renderMatrix, objectMatrix, worldMatrix, viewMatrix, projMatrix;
		Matrix[] bones;
		
		
		
		
		// Constructor
		public Game1()
		{
			Window.Title = "XNA Shader Programming Tutorial 24 - Bloom post process";
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			m_Model = null;
			m_ModelA = null;
		}
		
		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			width = graphics.GraphicsDevice.Viewport.Width;
			height = graphics.GraphicsDevice.Viewport.Height;
			
			// Set worldMatrix to Identity
			worldMatrix = Matrix.Identity;
			
			float aspectRatio = (float)width / (float)height;
			float FieldOfView = (float)Math.PI / 2, NearPlane = 1.0f, FarPlane = 1000.0f;
			projMatrix = Matrix.CreatePerspectiveFieldOfView(FieldOfView, aspectRatio, NearPlane, FarPlane);
			
			// Load and compile our Shader into our Effect instance.
			effect = Content.Load<Effect>("Shader.xnb");
			effectBlur = Content.Load<Effect>("Blur.xnb");
			effectBloom = Content.Load<Effect>("Bloom.xnb");
			effectCombine = Content.Load<Effect>("BloomCombine.xnb");
			
			// Load textures
			colorMap = Content.Load<Texture2D>("model_diff.xnb");
			normalMap = Content.Load<Texture2D>("model_norm.xnb");
			glossMap = Content.Load<Texture2D>("model_spec.xnb");
			alphaMap = Content.Load<Texture2D>("model_alpha.xnb");
			
			// Vertex declaration for rendering our 3D model.
//			graphics.GraphicsDevice.VertexDeclaration = new VertexDeclaration(VertexPositionNormalTextureTangentBinormal.VertexElements);
			graphics.GraphicsDevice.RasterizerState.CullMode = CullMode.None;
			
			
			base.Initialize();
		}
		
		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			spriteBatch = new SpriteBatch(GraphicsDevice);
			
			// Load our 3D model and transform bones.
			m_Model = Content.Load<Model>("Object.xnb");
			m_ModelA = Content.Load<Model>("ObjectA.xnb");
			bones = new Matrix[this.m_Model.Bones.Count];
			this.m_Model.CopyAbsoluteBoneTransformsTo(bones);
			
			PresentationParameters pp = graphics.GraphicsDevice.PresentationParameters;
			renderTarget = new RenderTarget2D(graphics.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, graphics.GraphicsDevice.DisplayMode.Format, graphics.GraphicsDevice.PresentationParameters.DepthStencilFormat);
			renderTargetBloom = new RenderTarget2D(graphics.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, graphics.GraphicsDevice.DisplayMode.Format, graphics.GraphicsDevice.PresentationParameters.DepthStencilFormat);
			renderTargetBlurBloom = new RenderTarget2D(graphics.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, graphics.GraphicsDevice.DisplayMode.Format, graphics.GraphicsDevice.PresentationParameters.DepthStencilFormat);
			renderTargetBlurIIBloom = new RenderTarget2D(graphics.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, graphics.GraphicsDevice.DisplayMode.Format, graphics.GraphicsDevice.PresentationParameters.DepthStencilFormat);
		}
		
		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
		}
		
		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			zHeight = 30;
			
			float m = (float)gameTime.ElapsedGameTime.Milliseconds / 1000;
			moveObject += m;
			
			// Move our object by doing some simple matrix calculations.
			objectMatrix = Matrix.CreateRotationX(0) * Matrix.CreateRotationY(-moveObject / 4);
			renderMatrix = Matrix.CreateScale(0.5f);
			viewMatrix = Matrix.CreateLookAt(new Vector3(x, y, zHeight), new Vector3(0, y, 0), Vector3.Up);
			
			renderMatrix = objectMatrix * renderMatrix;
			
			base.Update(gameTime);
		}
		
		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			////////////////////////////////////////
			// Render the normal scene to a texture
			graphics.GraphicsDevice.SetRenderTarget(renderTarget);
			graphics.GraphicsDevice.Clear(Color.Black);
			
			
			// Use the AmbientLight technique from Shader.fx. You can have multiple techniques in a effect file. If you don't specify
			// what technique you want to use, it will choose the first one by default.
			effect.CurrentTechnique = effect.Techniques["GlossMap"];
			
			// Begin our effect
//			effect.Begin();
			
			
			// A shader can have multiple passes, be sure to loop trough each of them.
			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				// Begin current pass
				pass.Apply();
				
				foreach (ModelMesh mesh in m_Model.Meshes)
				{
					foreach (ModelMeshPart part in mesh.MeshParts)
					{
						// calculate our worldMatrix..
						worldMatrix = bones[mesh.ParentBone.Index] * renderMatrix;
						
						Vector4 vecEye = new Vector4(x, y, zHeight, 0);
						
						
						// .. and pass it into our shader.
						// To access a parameter defined in our shader file ( Shader.fx ), use effectObject.Parameters["variableName"]
						Matrix worldInverse = Matrix.Invert(worldMatrix);
						Vector4 vLightDirection = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
						effect.Parameters["matWorldViewProj"].SetValue(worldMatrix * viewMatrix * projMatrix);
						effect.Parameters["matWorld"].SetValue(worldMatrix);
						effect.Parameters["vecEye"].SetValue(vecEye);
						effect.Parameters["vecLightDir"].SetValue(vLightDirection);
						effect.Parameters["ColorMap"].SetValue(colorMap);
						effect.Parameters["NormalMap"].SetValue(normalMap);
						effect.Parameters["GlossMap"].SetValue(glossMap);
						effect.Parameters["AlphaMap"].SetValue(alphaMap);
						
						
						
						// Render our meshpart
						GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
						graphics.GraphicsDevice.Indices = part.IndexBuffer;
						graphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						                                              0, 0, part.NumVertices,
						                                              part.StartIndex, part.PrimitiveCount);
					}
				}

			}
			// Stop using this effect
			effect.End();
			
			
			graphics.GraphicsDevice.SetRenderTarget(null);
			SceneTexture = renderTarget;
			
			
			
			////////////////////////////////////////
			// Render the bright areas of the scene in SceneTexture to a new texture
			graphics.GraphicsDevice.SetRenderTarget(renderTargetBloom);
			graphics.GraphicsDevice.Clear(Color.Black);
			
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
			{
				// Apply the post process shader
				effectBloom.CurrentTechnique.Passes[0].Apply();
				spriteBatch.Draw(SceneTexture, new Rectangle(0, 0, 800, 600), Color.White);
			}
			spriteBatch.End();
			
			graphics.GraphicsDevice.SetRenderTarget(null);
			BloomTexture = renderTargetBloom;
			
			
			////////////////////////////////////////
			// Blur the bright areas in the BloomTexture, making them "glow"
			graphics.GraphicsDevice.SetRenderTarget(renderTargetBlurBloom);
			graphics.GraphicsDevice.Clear(Color.Black);
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
			{
				// Apply the post process shader
				effectBlur.CurrentTechnique.Passes[0].Apply();
						spriteBatch.Draw(BloomTexture, new Rectangle(0, 0, 800, 600), Color.White);
						
			}
			spriteBatch.End();
			graphics.GraphicsDevice.SetRenderTarget(null);
			BlurBloomTexture = renderTargetBlurBloom;
			
			////////////////////////////////////////
			// Blur the bright areas in the BloomTexture a 2nd time, making them "glow" even more
			graphics.GraphicsDevice.SetRenderTarget(renderTargetBlurIIBloom);
			graphics.GraphicsDevice.Clear(Color.Black);
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
			{
				// Apply the post process shader
				effectBlur.CurrentTechnique.Passes[0].Apply();
						spriteBatch.Draw(BlurBloomTexture, new Rectangle(0, 0, 800, 600), Color.White);
						
			}
			spriteBatch.End();
			graphics.GraphicsDevice.SetRenderTarget(null);
			BlurIIBloomTexture = renderTargetBlurIIBloom;
			
			
			graphics.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkSlateBlue, 1.0f, 0);
			
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
			{
				// Apply the post process shader
				effectCombine.CurrentTechnique.Passes[0].Apply();
						effectCombine.Parameters["ColorMap"].SetValue(SceneTexture);
						spriteBatch.Draw(BlurIIBloomTexture, new Rectangle(0, 0, 800, 600), Color.White);
						
			}
			spriteBatch.End();

			
			base.Draw(gameTime);
		}
	}
}
