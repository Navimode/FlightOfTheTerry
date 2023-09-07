using Sandbox;
using System;
using System.Linq;

namespace JetpackGame.Player;

partial class JetpackPlayer : AnimatedEntity
{
	ModelEntity Corpse;
	static EntityLimit RagdollLimit = new EntityLimit { MaxTotal = 20 };

	[ClientRpc]
	void BecomeRagdollOnClient( Vector3 force, int forceBone )
	{
		var ent = new ModelEntity();
		ent.Position = Position;
		ent.Rotation = Rotation;
		ent.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		ent.CopyFrom( this );
		ent.CopyBonesFrom( this );
		ent.SetRagdollVelocityFrom( this );
		ent.DeleteAsync( 20.0f );

		// Copy the clothes over
		foreach ( var child in Children )
		{
			if ( !child.Tags.Has( "clothes" ) )
				continue;

			if ( child is ModelEntity e )
			{
				var clothing = new ModelEntity();
				clothing.CopyFrom( e );
				clothing.SetParent( ent, true );
			}
		}

		ent.PhysicsGroup.AddVelocity( force );

		if ( forceBone >= 0 )
		{
			var body = ent.GetBonePhysicsBody( forceBone );
			if ( body != null )
			{
				body.ApplyForce( force * 1000 );
			}
			else
			{
				ent.PhysicsGroup.AddVelocity( force );
			}
		}

		Corpse = ent;
		RagdollLimit.Watch( ent );
	}
}
