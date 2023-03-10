using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RelEcs;

namespace MonoGameExample;

public class Game1 : Game
{
    readonly GraphicsDeviceManager _graphics;

    readonly World _world = new();

    readonly SystemGroup _initSystems = new();
    readonly SystemGroup _updateSystems = new();
    readonly SystemGroup _renderSystems = new();
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _world.AddElement((Game)this);
        _world.AddElement(_graphics);
        _world.AddElement(GraphicsDevice);
        _world.AddElement(new SpriteBatch(GraphicsDevice));

        _initSystems.Add(new InitSystem());
        
        _updateSystems.Add(new InputSystem())
            .Add(new AnimationSystem())
            .Add(new MoveSystem());
        
        _renderSystems.Add(new RenderSystem());
        
        _initSystems.Run(_world);
        
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        _world.AddOrReplaceElement(gameTime);
        
        _updateSystems.Run(_world);
        
        base.Update(gameTime);

        _renderSystems.Run(_world);   
        
        _world.Tick();
    }
}

public class Position
{
    public Vector2 Value;
}

public class Velocity
{
    public Vector2 Value;
}

public class Sprite
{
    public bool Centered = true;
    public Texture2D Texture;
}

public class Animation
{
    public Texture2D[] Frames;
    public float FrameTime;
    public float Time;
}

public class InitSystem : ISystem
{
    readonly Random _random = new();
    
    public void Run(World world)
    {
        world.Spawn()
            .Add(new Sprite { Texture = world.LoadTexture2D("Content/icon.png") })
            .Add(new Position { Value = new Vector2(50, 50) })
            .Add(new Velocity { Value = new Vector2(_random.Next(-10, 10), _random.Next(-10, 10)) });
    }
}

public class InputSystem : ISystem
{
    readonly Random _random = new();
    
    public void Run(World world)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Space))
        {
            world.Spawn()
                .Add(new Sprite { Texture = world.LoadTexture2D("Content/icon.png") })
                .Add(new Animation
                {
                    FrameTime = 0.15f,
                    Frames = new[]
                    {
                        world.LoadTexture2D("Content/icon.png"),
                        world.LoadTexture2D("Content/icon1.png"),
                        world.LoadTexture2D("Content/icon2.png"),
                        world.LoadTexture2D("Content/icon3.png"),
                        world.LoadTexture2D("Content/icon2.png"),
                        world.LoadTexture2D("Content/icon1.png"),
                    }
                })
                .Add(new Position { Value = new Vector2(50, 50) })
                .Add(new Velocity { Value = new Vector2(_random.Next(-100, 100), _random.Next(-100, 100)) });
        }
    }
}

public class MoveSystem : ISystem
{
    public void Run(World world)
    {
        var device = world.GetElement<GraphicsDevice>();
        
        var gameTime = world.GetElement<GameTime>();
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        var height = device.Viewport.Height;
        var width = device.Viewport.Width;

        foreach (var (pos, vel) in world.Query<Position, Velocity>().Build())
        {
            pos.Value += vel.Value * delta;

            if (pos.Value.X < 0 || pos.Value.X > width)
            {
                vel.Value.X = -vel.Value.X;
            }
            
            if (pos.Value.Y < 0 || pos.Value.Y > height)
            {
                vel.Value.Y = -vel.Value.Y;
            }
            
            pos.Value.X = Math.Clamp(pos.Value.X, 0, width);
            pos.Value.Y = Math.Clamp(pos.Value.Y, 0, height);
        }
    }
}

public class AnimationSystem : ISystem
{
    public void Run(World world)
    {
        var gameTime = world.GetElement<GameTime>();
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        var query = world.Query<Sprite, Animation>().Build();

        foreach (var (sprite, animation) in query)
        {
            animation.Time += delta;
            var maxTime = animation.FrameTime * animation.Frames.Length;

            if (animation.Time > maxTime)
            {
                animation.Time -= maxTime;
            }
            
            var index = (int)(animation.Time / animation.FrameTime);
            var texture = animation.Frames[index];

            sprite.Texture = texture;
        }
    }
}

public class RenderSystem : ISystem
{
    public void Run(World world)
    {
        var device = world.GetElement<GraphicsDevice>();
        var spriteBatch = world.GetElement<SpriteBatch>();
        var query = world.Query<Sprite, Position>().Build();
        
        device.Clear(Color.CornflowerBlue);
        
        spriteBatch.Begin();
        
        foreach (var (sprite, pos) in query)
        {
            var offset = sprite.Centered
                ? new Vector2(sprite.Texture.Width / 2f, sprite.Texture.Width / 2f)
                : Vector2.Zero;
            
            spriteBatch.Draw(sprite.Texture, pos.Value - offset, Color.White);
        }
        
        spriteBatch.End();
    }
}

public static class WorldExtensions
{
    public static Texture2D LoadTexture2D(this World world, string path)
    {
        var device = world.GetElement<GraphicsDevice>();
        var texture = Texture2D.FromFile(device, path);
        return texture;
    }
}