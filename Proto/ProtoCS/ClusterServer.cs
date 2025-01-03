// Generated by sprotodump. DO NOT EDIT!
// source: C:\Users\zengzhiwei\SocketServer\Tools\sproto2cs\..\\..\\Proto\\ProtoFile\\ClusterServer.sproto

using System;
using Sproto;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NetSprotoType { 
	public class ClusterServer_Init : SprotoTypeBase {
		private static int max_field_count = 1;
		
		
		private Int64 _tcp_server_id; // tag 0
		public Int64 tcp_server_id {
			get { return _tcp_server_id; }
			set { base.has_field.set_field (0, true); _tcp_server_id = value; }
		}
		public bool HasTcp_server_id {
			get { return base.has_field.has_field (0); }
		}

		public ClusterServer_Init () : base(max_field_count) {}

		public ClusterServer_Init (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.tcp_server_id = base.deserialize.read_integer ();
					break;
				default:
					base.deserialize.read_unknow_data ();
					break;
				}
			}
		}

		public override int encode (SprotoStream stream) {
			base.serialize.open (stream);

			if (base.has_field.has_field (0)) {
				base.serialize.write_integer (this.tcp_server_id, 0);
			}

			return base.serialize.close ();
		}
	}


}

