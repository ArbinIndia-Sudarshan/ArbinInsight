using ArbinInsight.Models;
using ArbinInsight.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MachineData> MachineDatas { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<TestProfile> TestProfiles { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Limit> Limits { get; set; }
        public DbSet<CANMessagePair> CANMessagePairs { get; set; }
        public DbSet<SMBMessagePair> SMBMessagePair { get; set; }
        public DbSet<TestList> TestList_Table { get; set; }
        public DbSet<PublisherNode> PublisherNodes { get; set; }
        public DbSet<InboxMessage> InboxMessages { get; set; }
        public DbSet<DeadLetterMessage> DeadLetterMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Test>().ToTable("Test");
            modelBuilder.Entity<Limit>().ToTable("Limit");
            modelBuilder.Entity<PublisherNode>().ToTable("PublisherNodes", "sync");
            modelBuilder.Entity<InboxMessage>().ToTable("InboxMessages", "sync");
            modelBuilder.Entity<DeadLetterMessage>().ToTable("DeadLetterMessages", "sync");

            modelBuilder.Entity<MachineData>()
                .HasIndex(x => new { x.PublisherNodeId, x.MachineId })
                .IsUnique()
                .HasFilter("[PublisherNodeId] IS NOT NULL");

            modelBuilder.Entity<Channel>()
                .HasIndex(x => new { x.MachineDataId, x.ChannelIndex })
                .IsUnique()
                .HasFilter("[MachineDataId] IS NOT NULL");

            modelBuilder.Entity<TestProfile>()
                .HasIndex(x => new { x.PublisherNodeId, x.SourceLocalId })
                .IsUnique()
                .HasFilter("[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            modelBuilder.Entity<Test>()
                .HasIndex(x => new { x.PublisherNodeId, x.SourceLocalId })
                .IsUnique()
                .HasFilter("[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            modelBuilder.Entity<Limit>()
                .HasIndex(x => new { x.PublisherNodeId, x.SourceLocalId })
                .IsUnique()
                .HasFilter("[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            modelBuilder.Entity<TestList>()
                .HasIndex(x => new { x.PublisherNodeId, x.SourceLocalId })
                .IsUnique()
                .HasFilter("[PublisherNodeId] IS NOT NULL AND [SourceLocalId] IS NOT NULL");

            modelBuilder.Entity<TestList>()
                .HasIndex(x => new { x.MachineDataId, x.ChannelId, x.Start_Date_Time });

            modelBuilder.Entity<InboxMessage>()
                .HasIndex(x => x.MessageId)
                .IsUnique();

            modelBuilder.Entity<InboxMessage>()
                .HasIndex(x => new { x.Status, x.ReceivedAtUtc });

            modelBuilder.Entity<PublisherNode>()
                .HasIndex(x => x.NodeCode)
                .IsUnique();

            modelBuilder.Entity<InboxMessage>()
                .Property(x => x.ReceivedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder.Entity<InboxMessage>()
                .Property(x => x.Status)
                .HasDefaultValue((byte)0);

            modelBuilder.Entity<DeadLetterMessage>()
                .Property(x => x.FailedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder.Entity<PublisherNode>()
                .Property(x => x.IsOnline)
                .HasDefaultValue(false);

            modelBuilder.Entity<PublisherNode>()
                .Property(x => x.CreatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder.Entity<PublisherNode>()
                .Property(x => x.UpdatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder.Entity<Test>()
                .HasOne(x => x.Channel)
                .WithMany()
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TestList>()
                .HasOne(x => x.Channel)
                .WithMany()
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TestList>()
                .HasOne(x => x.TestProfile)
                .WithMany()
                .HasForeignKey(x => x.TestProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TestList>()
                .HasOne(x => x.MachineData)
                .WithMany(x => x.TestLists)
                .HasForeignKey(x => x.MachineDataId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
