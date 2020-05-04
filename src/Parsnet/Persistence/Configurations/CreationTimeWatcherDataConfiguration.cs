using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parsnet.FileWatchers.CreationTimeWatcher.Data;

namespace Parsnet.Persistence.Configurations
{
    public class CreationTimeWatcherDataConfiguration : IEntityTypeConfiguration<CreationTimeWatcherData>
    {
        public void Configure(EntityTypeBuilder<CreationTimeWatcherData> builder)
        {
            builder.Property(ct => ct.ParserName)
                .IsRequired();

            builder.Property(ct => ct.LastCreationTimeUtc)
                .IsRequired()
                .HasDefaultValue(DateTime.MinValue.ToUniversalTime().Ticks);
        }
    }
}
