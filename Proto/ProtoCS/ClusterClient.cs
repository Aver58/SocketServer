// Generated by sprotodump. DO NOT EDIT!
// source: C:\Users\zengzhiwei\SocketServer\Tools\sproto2cs\..\\..\\Proto\\ProtoFile\\ClusterClient.sproto

using System;
using Sproto;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NetSprotoType { 
	public class ClusterClientRequest : SprotoTypeBase {
		private static int max_field_count = 4;
		
		
		private string _remoteNode; // tag 0
		public string remoteNode {
			get { return _remoteNode; }
			set { base.has_field.set_field (0, true); _remoteNode = value; }
		}
		public bool HasRemoteNode {
			get { return base.has_field.has_field (0); }
		}

		private string _remoteService; // tag 1
		public string remoteService {
			get { return _remoteService; }
			set { base.has_field.set_field (1, true); _remoteService = value; }
		}
		public bool HasRemoteService {
			get { return base.has_field.has_field (1); }
		}

		private string _method; // tag 2
		public string method {
			get { return _method; }
			set { base.has_field.set_field (2, true); _method = value; }
		}
		public bool HasMethod {
			get { return base.has_field.has_field (2); }
		}

		private string _param; // tag 3
		public string param {
			get { return _param; }
			set { base.has_field.set_field (3, true); _param = value; }
		}
		public bool HasParam {
			get { return base.has_field.has_field (3); }
		}

		public ClusterClientRequest () : base(max_field_count) {}

		public ClusterClientRequest (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.remoteNode = base.deserialize.read_string ();
					break;
				case 1:
					this.remoteService = base.deserialize.read_string ();
					break;
				case 2:
					this.method = base.deserialize.read_string ();
					break;
				case 3:
					this.param = base.deserialize.read_string ();
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
				base.serialize.write_string (this.remoteNode, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_string (this.remoteService, 1);
			}

			if (base.has_field.has_field (2)) {
				base.serialize.write_string (this.method, 2);
			}

			if (base.has_field.has_field (3)) {
				base.serialize.write_string (this.param, 3);
			}

			return base.serialize.close ();
		}
	}


	public class ClusterClientSocketConnected : SprotoTypeBase {
		private static int max_field_count = 3;
		
		
		private Int64 _connection; // tag 0
		public Int64 connection {
			get { return _connection; }
			set { base.has_field.set_field (0, true); _connection = value; }
		}
		public bool HasConnection {
			get { return base.has_field.has_field (0); }
		}

		private string _ip; // tag 1
		public string ip {
			get { return _ip; }
			set { base.has_field.set_field (1, true); _ip = value; }
		}
		public bool HasIp {
			get { return base.has_field.has_field (1); }
		}

		private Int64 _port; // tag 2
		public Int64 port {
			get { return _port; }
			set { base.has_field.set_field (2, true); _port = value; }
		}
		public bool HasPort {
			get { return base.has_field.has_field (2); }
		}

		public ClusterClientSocketConnected () : base(max_field_count) {}

		public ClusterClientSocketConnected (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.connection = base.deserialize.read_integer ();
					break;
				case 1:
					this.ip = base.deserialize.read_string ();
					break;
				case 2:
					this.port = base.deserialize.read_integer ();
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
				base.serialize.write_integer (this.connection, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_string (this.ip, 1);
			}

			if (base.has_field.has_field (2)) {
				base.serialize.write_integer (this.port, 2);
			}

			return base.serialize.close ();
		}
	}


	public class ClusterClient_Init : SprotoTypeBase {
		private static int max_field_count = 2;
		
		
		private string _cluster_config; // tag 0
		public string cluster_config {
			get { return _cluster_config; }
			set { base.has_field.set_field (0, true); _cluster_config = value; }
		}
		public bool HasCluster_config {
			get { return base.has_field.has_field (0); }
		}

		private Int64 _tcp_client_id; // tag 1
		public Int64 tcp_client_id {
			get { return _tcp_client_id; }
			set { base.has_field.set_field (1, true); _tcp_client_id = value; }
		}
		public bool HasTcp_client_id {
			get { return base.has_field.has_field (1); }
		}

		public ClusterClient_Init () : base(max_field_count) {}

		public ClusterClient_Init (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.cluster_config = base.deserialize.read_string ();
					break;
				case 1:
					this.tcp_client_id = base.deserialize.read_integer ();
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
				base.serialize.write_string (this.cluster_config, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_integer (this.tcp_client_id, 1);
			}

			return base.serialize.close ();
		}
	}


}

