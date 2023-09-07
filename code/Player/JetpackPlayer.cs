using Sandbox;
using System;
using System.ComponentModel;
using System.Linq;

namespace JetpackGame.Player;

enum JetpackStance
{
	Hover,
	Forward
}

partial class JetpackPlayer : AnimatedEntity
{
	[BindComponent] public JetpackController Controller { get; }
	Particles ExhaustParticles;
	public Capsule Capsule => new
	(
		//Transform.ToWorld( Model.GetAttachment( "rear" ).Value ).Position,
		//Transform.ToWorld( Model.GetAttachment( "tip" ).Value ).Position, 10
		Model.GetAttachment( "rear" ).Value.Position,
		Model.GetAttachment( "tip" ).Value.Position, 10
	);
	[ClientInput, Browsable( false )] public Vector3 InputDirection { get; protected set; }
	[ClientInput, Browsable( false )] public Angles ViewAngles { get; set; }
	public JetpackStance Stance { get; set; }
	public bool IsDead => LifeState == LifeState.Dead;
	DamageInfo LastDamage;
	TimeSince TimeSinceDied = 0;

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();

		//Model = Model.Load( "models/sbox_props/watermelon/watermelon.vmdl" );
		SetModel( "models/dev_arrow.vmdl" );
		//SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		//EnableAllCollisions = true;
		Tags.Add( "player" );
		ExhaustParticles = Particles.Create( "particles/jetpack_exhaust.vpcf", this, "rear" );
	}

	public void Respawn()
	{
		Game.AssertServer();
		LifeState = LifeState.Alive;
		Velocity = Vector3.Zero;

		EnableDrawing = true;
		EnableAllCollisions = true;

		//foreach ( var child in Children.OfType<ModelEntity>() )
		//	child.EnableDrawing = true;

		Components.Create<JetpackController>();
		//Components.Create<StrandedAnimator>();

		//SetActiveWeapon( new Pistol() );

		JetpackGame1.MoveToSpawnPoint( this );
		ResetInterpolation();
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		if ( Game.IsServer )
		{
			if ( IsDead )
			{
				if ( TimeSinceDied > 3 )
				{
					Respawn();
				}
				return;
			}
		}

		Controller?.Simulate( cl );

		//ExhaustParticles.Simulating = GroundEntity is not null ? false : true;

		if ( !Game.IsServer ) return;

		if ( Input.Pressed( "menu" ) )
		{
			OnKilled();
		}

		if ( Input.Pressed( "attack1" ) )
		{
			var ragdoll = new ModelEntity();
			ragdoll.SetModel( "models/citizen/citizen.vmdl" );
			ragdoll.Position = Position + Rotation.Forward * 40;
			ragdoll.Rotation = Rotation.LookAt( Vector3.Random.Normal );
			ragdoll.SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
			ragdoll.PhysicsGroup.Velocity = Rotation.Forward * 1000;
		}
	}

	/// <summary>
	/// Applies flashbang-like ear ringing effect to the player.
	/// </summary>
	/// <param name="strength">Can be approximately treated as duration in seconds.</param>
	[ClientRpc]
	public void Deafen( float strength )
	{
		Audio.SetEffect( "flashbang", strength, velocity: 20.0f, fadeOut: 4.0f * strength );
	}

	/// <summary>
	/// Overload to simply kill the player
	/// </summary>
	public override void OnKilled()
	{
		OnKilled( DamageInfo.Generic( 9999 )
			.WithAttacker( Game.WorldEntity ) );
	}

	/// <summary>
	/// Kill the player, optionally supplying a damageinfo for context (blast damage, etc.)
	/// </summary>
	/// <param name="info"></param>
	public virtual void OnKilled( DamageInfo info )
	{
		if ( IsDead ) return;

		if ( Health <= 0f )
			Health = 0f;

		this.ProceduralHitReaction( info );

		// These are needed for the default killfeed
		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;

		if ( info.HasTag( "blast" ) )
			Deafen( To.Single( Client ), info.Damage.LerpInverse( 0, 60 ) );

		// Add a score to the killer
		if ( IsDead && info.Attacker != null )
		{
			if ( info.Attacker.Client != null && info.Attacker != this )
				info.Attacker.Client.AddInt( "kills" );
		}

		// Registers to the game that this ent died
		GameManager.Current?.OnKilled( this );

		if ( LastDamage.HasTag( "blast" ) )
		{
			using ( Prediction.Off() )
			{
				var particles = Particles.Create( "particles/gib.vpcf" );
				particles?.SetPosition( 0, Position + Vector3.Up * 40 );
			}
		}
		else
		{
			BecomeRagdollOnClient( LastDamage.Force, LastDamage.BoneIndex );
		}

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children.OfType<ModelEntity>() )
			child.EnableDrawing = false;

		Client?.AddInt( "deaths", 1 );
		LifeState = LifeState.Dead;
		TimeSinceDied = 0;
		Components.RemoveAll();
		//Delete();
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		base.OnPhysicsCollision( eventData );
	}

	public TraceResult TraceCapsule( Vector3 start, Vector3 end )
	{
		TraceResult tr = Trace.Capsule( Capsule, start, end )
			.WithAnyTags( "solid", "playerclip", "passbullets" )
			.Ignore( this )
			.Run();

		return tr;
			
	}
}
