#!/usr/bin/perl

@relations = (
  'MONO_NO_RELATION',  # 000
  'MONO_EQ_RELATION',  # 001
  'MONO_LT_RELATION',  # 010
  'MONO_LE_RELATION',  # 011 (MONO_LT_RELATION|MONO_EQ_RELATION)
  'MONO_GT_RELATION',  # 100
  'MONO_GE_RELATION',  # 101 (MONO_GT_RELATION|MONO_EQ_RELATION)
  'MONO_NE_RELATION',  # 110 (MONO_LT_RELATION|MONO_GT_RELATION)
  'MONO_ANY_RELATION', # 111 (MONO_EQ_RELATION|MONO_LT_RELATION|MONO_GT_RELATION)
);

sub build_propagated_relation
{
  my ( $main_relation, $related_relation ) = @_;
  my $result = 'MONO_UNKNOWN_RELATION';
  
  if ( ($main_relation eq 'MONO_EQ_RELATION') && ($related_relation ne 'MONO_NO_RELATION') )
  {
    $result = $related_relation;
  }
  elsif ( $main_relation eq 'MONO_LT_RELATION' )
  {
    if ( $related_relation eq 'MONO_EQ_RELATION' )
    {
      $result = 'MONO_LT_RELATION';
    }
    elsif ( $related_relation eq 'MONO_LT_RELATION' )
    {
      $result = 'MONO_LT_RELATION';
    }
    elsif ( $related_relation eq 'MONO_LE_RELATION' )
    {
      $result = 'MONO_LT_RELATION';
    }
    else
    {
      $result = 'MONO_ANY_RELATION';
    }
  }
  elsif ( $main_relation eq 'MONO_LE_RELATION' )
  {
    if ( $related_relation eq 'MONO_EQ_RELATION' )
    {
      $result = 'MONO_LE_RELATION';
    }
    elsif ( $related_relation eq 'MONO_LT_RELATION' )
    {
      $result = 'MONO_LT_RELATION';
    }
    elsif ( $related_relation eq 'MONO_LE_RELATION' )
    {
      $result = 'MONO_LE_RELATION';
    }
    else
    {
      $result = 'MONO_ANY_RELATION';
    }
  }
  
  elsif ( $main_relation eq 'MONO_GT_RELATION' )
  {
    if ( $related_relation eq 'MONO_EQ_RELATION' )
    {
      $result = 'MONO_GT_RELATION';
    }
    elsif ( $related_relation eq 'MONO_GT_RELATION' )
    {
      $result = 'MONO_GT_RELATION';
    }
    elsif ( $related_relation eq 'MONO_GE_RELATION' )
    {
      $result = 'MONO_GT_RELATION';
    }
    else
    {
      $result = 'MONO_ANY_RELATION';
    }
  }
  elsif ( $main_relation eq 'MONO_GE_RELATION' )
  {
    if ( $related_relation eq 'MONO_EQ_RELATION' )
    {
      $result = 'MONO_GE_RELATION';
    }
    elsif ( $related_relation eq 'MONO_GT_RELATION' )
    {
      $result = 'MONO_GT_RELATION';
    }
    elsif ( $related_relation eq 'MONO_GE_RELATION' )
    {
      $result = 'MONO_GE_RELATION';
    }
    else
    {
      $result = 'MONO_ANY_RELATION';
    }
  }
  else
  {
    $result = 'MONO_ANY_RELATION';
  }
  
  $result;
}

open FILE, ">propagated_relations_table.def";

for ( my $mr = 0; $mr < 8; $mr++ )
{
  for ( my $pr = 0; $pr < 8; $pr++ )
  {
    my $propagated_relation = &build_propagated_relation( $relations[$mr], $relations[$pr] );
    print FILE "  $propagated_relation, /* $relations[$mr] - $relations[$pr] */\n";
  }
}

close FILE;
